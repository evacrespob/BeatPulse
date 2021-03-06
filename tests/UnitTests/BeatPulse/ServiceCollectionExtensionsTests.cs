﻿using BeatPulse.Core;
using FluentAssertions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BeatPulse
{
    public class service_collection_extensions_AddBeatPulse_should
    {
        private readonly IServiceProvider _serviceProvider;

        public service_collection_extensions_AddBeatPulse_should()
        {
            _serviceProvider = WebHost.CreateDefaultBuilder()
                .UseStartup<DefaultStartup>()
                .UseBeatPulse()
                .Build()
                .Services;
        }


        [Fact]
        public void register_beat_pulse_service()
        {
            _serviceProvider.GetService<IBeatPulseService>()
                .Should()
                .BeOfType<BeatPulseService>();
        }

        [Fact]
        public void register_beat_pulse_context()
        {
            _serviceProvider.GetService<BeatPulseContext>()
                .Should()
                .BeOfType<BeatPulseContext>();
        }

        [Fact]
        public void set_valid_serviceprovider_to_context_to_allow_resolve_liveness()
        {
            // Need to resolve IBeatPulseService because its 
            // it who injects IServiceProvider to context.

            var beatPulseService = _serviceProvider.GetService<IBeatPulseService>();
            var beatPulseContext = _serviceProvider.GetService<BeatPulseContext>();

            string path2;
            beatPulseContext.FindLiveness(nameof(path2))
                 .Should()
                 .NotBeNull();
        }

        [Fact]
        public void set_valid_serviceprovider_to_context_to_allow_resolve_trackers()
        {
            // Need to resolve IBeatPulseService because its 
            // it who injects IServiceProvider to context.

            var beatPulseService = _serviceProvider.GetService<IBeatPulseService>();
            var beatPulseContext = _serviceProvider.GetService<BeatPulseContext>();

            var testTrackerName = nameof(TestTracker);
            var testTrackerWithDependenciesName = nameof(TestTrackerWithDependencies);

            beatPulseContext.AllTrackers
                .Where(tracker => tracker.Name.Equals(testTrackerName))
                .SingleOrDefault()
                .Should()
                .NotBeNull();

            beatPulseContext.AllTrackers
               .Where(tracker => tracker.Name.Equals(testTrackerWithDependenciesName))
               .SingleOrDefault()
               .Should()
               .NotBeNull();
        }

        [Fact]
        public void execute_the_context_setup()
        {
            var beatPulseContext = _serviceProvider.GetService<BeatPulseContext>();

            string path;

            beatPulseContext.FindLiveness(nameof(path))
                .Should()
                .NotBeNull();

        }

        class FooService { }

        class DefaultStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                string name;
                string path;
                string path2;

                var taskResult = Task.FromResult((string.Empty, true));

                services.AddBeatPulse(context =>
                {
                    //liveness 

                    context.AddLiveness(new ActionLiveness(nameof(name), nameof(path), (httpcontext, cancellationToken) => taskResult));
                    context.AddLiveness(nameof(path2), sp =>
                    {
                        var service = sp.GetRequiredService<FooService>();
                        return new ActionLiveness(nameof(path2), nameof(path2), (httpcontext, cancellationToken) => taskResult);
                    });

                    //trackers

                    context.AddTracker(new TestTracker());
                    context.AddTracker(nameof(TestTrackerWithDependencies), sp =>
                    {
                        var fooService = sp.GetRequiredService<FooService>();

                        return new TestTrackerWithDependencies(fooService);
                    });
                });

                services.AddTransient<FooService>();
            }

            public void Configure(IApplicationBuilder app)
            {
            }
        }

        class TestTrackerWithDependencies : IBeatPulseTracker
        {
            private FooService _fooService;

            public string Name => nameof(TestTrackerWithDependencies);

            public TestTrackerWithDependencies(FooService fooService)
            {
                _fooService = fooService;
            }

            public Task Track(LivenessResult response)
            {
                return Task.CompletedTask;
            }
        }

        class TestTracker : IBeatPulseTracker
        {
            public string Name => nameof(TestTracker);

            public Task Track(LivenessResult response)
            {
                return Task.CompletedTask;
            }
        }
    }
}
