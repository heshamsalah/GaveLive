var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gavellivedb = postgres.AddDatabase("gavellive");

builder.AddProject<Projects.Api>("api")
    .WithReference(gavellivedb)
    .WaitFor(gavellivedb);

builder.Build().Run();