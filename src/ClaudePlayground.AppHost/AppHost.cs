var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB container without authentication for local development
var mongodb = builder.AddContainer("mongodb", "mongo", "7.0")
    .WithBindMount("mongodb-data", "/data/db")
    .WithEndpoint(port: 27017, targetPort: 27017, name: "tcp")
    .WithArgs("--noauth");

// Add Redis for caching
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add the API project with MongoDB connection string
var mongoEndpoint = mongodb.GetEndpoint("tcp");
builder.AddProject<Projects.ClaudePlayground_Api>("api")
    .WithEnvironment("ConnectionStrings__mongodb",
        ReferenceExpression.Create($"mongodb://{mongoEndpoint.Property(EndpointProperty.Host)}:{mongoEndpoint.Property(EndpointProperty.Port)}"))
    .WithReference(redis)
    .WaitFor(mongodb)
    .WaitFor(redis);

builder.Build().Run();
