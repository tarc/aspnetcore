// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Api;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Routing.FunctionalTests
{
    public class MapActionTest
    {
        [Fact]
        public async Task MapAction_FromBodyWorksWithJsonPayload()
        {
            [CustomRouteMetadata(Pattern = "/EchoTodo", Methods = new[] { "POST" })]
            Todo EchoTodo([FromBody] Todo todo) => todo;

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseEndpoints(b => b.MapAction((Func<Todo, Todo>)EchoTodo));
                        })
                        .UseTestServer();
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .Build();

            using var server = host.GetTestServer();
            await host.StartAsync();
            var client = server.CreateClient();

            var todo = new Todo
            {
                Name = "Write tests!"
            };

            var response = await client.PostAsJsonAsync("/EchoTodo", todo);
            response.EnsureSuccessStatusCode();

            var echoedTodo = await response.Content.ReadFromJsonAsync<Todo>();

            Assert.Equal(todo.Name, echoedTodo?.Name);
        }

        private class Todo
        {
            public int Id { get; set; }
            public string Name { get; set; } = "Todo";
            public bool IsComplete { get; set; }
        }

        private class FromBodyAttribute : Attribute, IFromBodyMetadata { }

        private class CustomRouteMetadataAttribute : Attribute, IRoutePatternMetadata, IHttpMethodMetadata, IRouteNameMetadata, IRouteOrderMetadata
        {
            public string Pattern { get; set; } = "/";

            public string? Name { get; set; }

            public int Order { get; set; } = 0;

            public string[] Methods { get; set; } = new[] { "GET" };

            string? IRoutePatternMetadata.RoutePattern => Pattern;

            string? IRouteNameMetadata.RouteName => Name;

            int? IRouteOrderMetadata.RouteOrder => Order;

            IReadOnlyList<string> IHttpMethodMetadata.HttpMethods => Methods;

            bool IHttpMethodMetadata.AcceptCorsPreflight => false;
        }
    }
}