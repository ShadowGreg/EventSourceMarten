using System.Runtime.Serialization;
using EventSourceMarten.Contracts.Responses;
using EventSourceMarten.Exception;
using EventSourceMarten.Marten;
using EventSourceMarten.Projections;
using EventSourceMarten.Services.Driver;
using EventSourceMarten.Validation;
using FastEndpoints;
using FastEndpoints.Swagger;
using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Npgsql;
using Serilog;
using Serilog.Formatting.Display;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Logger(lc => lc
                                 .Filter.ByExcluding(e => e.Properties.ContainsKey("IsSql"))
                                 .WriteTo.Console(
                                      outputTemplate:
                                      "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                                  )
             )
            .WriteTo.Logger(lc => lc
                                 .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("IsSql"))
                                 .WriteTo.Console(new AnsiSqlFormatter())
             )
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddFastEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
            {
                s.Title = "Documentation";
                s.Version = "1.0.0";
            };
    });

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidDataContractException("DefaultConnection connection string not found");

builder.Services.AddSingleton(sp =>
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(cs);
        dataSourceBuilder.UseLoggerFactory(loggerFactory);
        return dataSourceBuilder.Build();
    });

builder.Services.AddMarten(options =>
                {
                    options.DatabaseSchemaName = "event_source";
                    options.Projections.Add<DriverProjection>(ProjectionLifecycle.Async);
                    options.Projections.Add<DriverHistoryProjection>(ProjectionLifecycle.Async);
                    options.AutoCreateSchemaObjects = AutoCreate.None;
#if DEBUG
                    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
#endif

                    options.ConfigureEventSourceSchema();
                    options.Logger(new SerilogMartenLogger(Log.Logger));
                }
        )
       .UseLightweightSessions()
       .UseNpgsqlDataSource()
       .AddAsyncDaemon(DaemonMode.HotCold)
        ;

builder.Services.AddScoped<IDriverService, DriverService>();

var app = builder.Build();
app.UseSerilogRequestLogging();

app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseFastEndpoints(x =>
    {
        x.Errors.ResponseBuilder = (failures, _, statusCode) =>
            {
                return new ValidationFailureResponse { Errors = failures.Select(y => y.ErrorMessage).ToList() };
            };
    });

app.UseSwaggerGen();
app.UseSwaggerUi();

app.Run();