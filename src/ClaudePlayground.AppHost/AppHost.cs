var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB container without authentication for local development
var mongodb = builder.AddContainer("mongodb", "mongo", "7.0")
    .WithBindMount("mongodb-data", "/data/db")
    .WithHttpEndpoint(port: 27017, targetPort: 27017, name: "tcp")
    .WithArgs("--noauth");

// Add Redis for caching
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add the API project
builder.AddProject<Projects.ClaudePlayground_Api>("api")
    .WithEnvironment("ConnectionStrings__mongodb", mongodb.GetEndpoint("tcp"))
    .WithReference(redis)
    .WaitFor(mongodb)
    .WaitFor(redis);

builder.Build().Run();
