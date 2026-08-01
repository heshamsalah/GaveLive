var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gavellivedb = postgres.AddDatabase("gavellive");

var cache = builder.AddRedis("cache")
    .WithDataVolume();

var messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume();

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume();

builder.AddProject<Projects.Api>("api")
    .WithReference(gavellivedb)
    .WaitFor(gavellivedb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.Build().Run();