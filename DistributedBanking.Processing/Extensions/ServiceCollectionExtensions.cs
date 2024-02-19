using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Data.Repositories.Base;
using DistributedBanking.Processing.Data.Repositories.Implementation;
using DistributedBanking.Processing.Domain.Models.Transaction;
using DistributedBanking.Processing.Domain.Options;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Domain.Services.Implementation;
using DistributedBanking.Processing.Listeners.Account;
using DistributedBanking.Processing.Listeners.Identity;
using DistributedBanking.Processing.Listeners.Transaction;
using Mapster;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Refit;
using Shared.Data.Converters;
using Shared.Data.Entities;
using Shared.Data.Services;
using Shared.Data.Services.Implementation.MongoDb;
using Shared.Kafka.Extensions;
using Shared.Kafka.Options;
using Shared.Messaging.Messages.Account;
using Shared.Messaging.Messages.Identity;
using Shared.Messaging.Messages.Identity.Registration;
using Shared.Messaging.Messages.Transaction;
using Shared.Redis.Extensions;
using System.Text.Json;
using TransactionalClock.Integration;
using TransactionalClock.Integration.DelegationHandlers;
using TransactionalClock.Integration.Options;
// ReSharper disable UnusedMethodReturnValue.Local

namespace DistributedBanking.Processing.Extensions;

public static class ServiceCollectionExtensions
{
    internal static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)));
        
        return services;
    }
    
    internal static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMapster();
        services.AddMemoryCache();

        services
            .AddMongoDatabase(configuration)
            .AddDataRepositories();

        services.AddTransactionalClockIntegration(configuration);
        
        services
            .AddTransient<IRolesManager, RolesManager>()
            .AddTransient<IUserManager, UserManager>()
            .AddTransient<IAccountService, AccountService>()
            .AddTransient<IPasswordHashingService, PasswordHashingService>()
            .AddTransient<IIdentityService, IdentityService>()
            .AddTransient<ITransactionService, TransactionService>();
        
        services.AddRedis(configuration);
        services.AddKafkaConsumers(configuration);
        
        return services;
    }
    
    private static IServiceCollection AddKafkaConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafkaConsumer<string, RoleCreationMessage>(configuration, KafkaTopicSource.RoleCreation);
        services.AddKafkaConsumer<string, UserRegistrationMessage>(configuration, KafkaTopicSource.CustomersRegistration);
        services.AddKafkaConsumer<string, WorkerRegistrationMessage>(configuration, KafkaTopicSource.WorkersRegistration);
        services.AddKafkaConsumer<string, CustomerInformationUpdateMessage>(configuration, KafkaTopicSource.CustomersUpdate);
        services.AddKafkaConsumer<string, EndUserDeletionMessage>(configuration, KafkaTopicSource.UsersDeletion);
        services.AddKafkaConsumer<string, AccountCreationMessage>(configuration, KafkaTopicSource.AccountCreation);
        services.AddKafkaConsumer<string, AccountDeletionMessage>(configuration, KafkaTopicSource.AccountDeletion);
        services.AddKafkaConsumer<string, TransactionMessage>(configuration, KafkaTopicSource.TransactionsCreation);
        
        return services;
    }
    
    internal static IServiceCollection AddBackgroundListeners(this IServiceCollection services)
    {
        services
            .AddHostedService<RoleCreationListener>()
            .AddHostedService<AccountCreationListener>()
            .AddHostedService<AccountDeletionListener>()
            .AddHostedService<CustomerInformationUpdateListener>()
            .AddHostedService<CustomerRegistrationListener>()
            .AddHostedService<EndUserDeletionListener>()
            .AddHostedService<WorkerRegistrationListener>()
            .AddHostedService<TransactionsListener>();

        return services;
    }
    
    private static IServiceCollection AddMapster(this IServiceCollection services)
    {
        TypeAdapterConfig<TransactionEntity, TransactionResponseModel>.NewConfig()
            .Map(dest => dest.Timestamp, src => src.DateTime);
        
        TypeAdapterConfig<ObjectId, string>.NewConfig()
            .MapWith(objectId => objectId.ToString());

        TypeAdapterConfig<string, ObjectId>.NewConfig()
            .MapWith(value => new ObjectId(value));

        TypeAdapterConfig<TransactionMessage, OneWaySecuredTransactionModel>.NewConfig()
            .Map(dest => dest.SecurityCode, src => src.SourceSecurityCode);

        TypeAdapterConfig<TransactionMessage, TwoWayTransactionModel>.NewConfig()
           .Map(dest => dest.SourceAccountSecurityCode, src => src.SourceSecurityCode);

        return services;
    }
    
    private static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
       var databaseOptions = configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>();
        ArgumentNullException.ThrowIfNull(databaseOptions);

        services.AddSingleton<IMongoDbFactory>(new MongoDbFactory(databaseOptions.ConnectionString, databaseOptions.DatabaseName));

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreIfNullConvention(true)
        };
        ConventionRegistry.Register("CamelCase_StringEnum_IgnoreNull_Convention", pack, _ => true);
        
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));

       return services;
    }
    
    private static IServiceCollection AddDataRepositories(this IServiceCollection services)
    {
        services.AddTransient(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
        
        services.AddTransient<IUsersRepository, UsersRepository>();
        services.AddTransient<IRolesRepository, RolesRepository>();
        services.AddTransient<IAccountsRepository, AccountsRepository>();
        services.AddTransient<ICustomersRepository, CustomersRepository>();
        services.AddTransient<IWorkersRepository, WorkersRepository>();
        services.AddTransient<ITransactionsRepository, TransactionsRepository>();
        
        return services;
    }
    
    private static IServiceCollection AddTransactionalClockIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var transactionalClockOptions = configuration.GetSection(nameof(TransactionalClockOptions)).Get<TransactionalClockOptions>();
        ArgumentNullException.ThrowIfNull(transactionalClockOptions);
       
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                Converters = { new ObjectIdJsonConverter() }
            })
        };

        var httpClientBuilder = services
            .AddRefitClient<ITransactionalClockClient>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(transactionalClockOptions.TransactionalClockHostUrl));
        
#if DEBUG
        services.TryAddScoped<HttpDebugLoggingHandler>();
        httpClientBuilder.AddHttpMessageHandler<HttpDebugLoggingHandler>();
#endif
        
        return services;
    }
}