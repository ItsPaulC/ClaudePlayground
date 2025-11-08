// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ClaudePlayground.Api.Settings;

public record RedisSettings
{
    public required string ConnectionString { get; set; }
    public required string InstanceName { get; set; }
}
