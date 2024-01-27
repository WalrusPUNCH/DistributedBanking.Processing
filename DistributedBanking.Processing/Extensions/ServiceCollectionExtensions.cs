using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Data.Repositories.Base;
using DistributedBanking.Processing.Data.Repositories.Implementation;
using DistributedBanking.Processing.Domain.Models.Transaction;
using DistributedBanking.Processing.Domain.Options;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Domain.Services.Implementation;
using DistributedBanking.Processing.Helpers;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Refit;
using Shared.Data.Converters;
using Shared.Data.Entities;
using Shared.Data.Services;
using Shared.Data.Services.Implementation.MongoDb;
using TransactionalClock.Integration;
using TransactionalClock.Integration.DelegationHandlers;
using TransactionalClock.Integration.Options;

namespace DistributedBanking.Processing.Extensions;

public static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();
        ArgumentNullException.ThrowIfNull(jwtOptions);
        
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services
            .AddRouting()
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });
        
        services
            .AddAuthorization()
            .AddEndpointsApiExplorer()
            .AddHttpContextAccessor()
            .AddMapster();
        
        return services;
    }
    
    internal static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = CustomInvalidModelStateResponseFactory.MakeFailedValidationResponse;
        });

        services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)));
        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
        
        return services;
    }
    
    internal static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMongoDatabase(configuration)
            .AddDataRepositories();

        services
            .AddTransient<IUserManager, UserManager>()
            .AddTransient<IAccountService, AccountService>()
            .AddTransient<IPasswordHashingService, PasswordHashingService>()
            .AddTransient<IIdentityService, IdentityService>()
            .AddTransient<ITransactionService, TransactionService>();

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
    
    public static IServiceCollection AddTransactionalClockIntegration(this IServiceCollection services, IConfiguration configuration)
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