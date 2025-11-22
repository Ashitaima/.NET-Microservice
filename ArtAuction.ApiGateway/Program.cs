var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults
builder.AddServiceDefaults();

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Use YARP
app.MapReverseProxy();

app.Run();
