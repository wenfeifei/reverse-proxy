// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SampleClient.Scenarios;

namespace SampleClient
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CommandLineArgs parsedArgs = null;
            try
            {
                parsedArgs = CommandLineArgs.Parse(args);
            }
            catch (ArgumentException)
            {
                // Do nothing, we will show help right after.
            }

            if (parsedArgs == null || parsedArgs.Help)
            {
                CommandLineArgs.ShowHelp();
                return 1;
            }

            var scenarioFactories = new Dictionary<string, Func<IScenario>>(StringComparer.OrdinalIgnoreCase) {
                {"Http1", () => new Http1Scenario()},
                {"Http2", () => new Http2Scenario()},
                {"RawUpgrade", () => new RawUpgradeScenario()},
                {"WebSockets", () => new WebSocketsScenario()},
                {"SessionAffinity", () => new SessionAffinityScenario()}
            };

            if (string.IsNullOrEmpty(parsedArgs.Scenario))
            {
                // Execute all scenarios
                var success = true;
                foreach (var kvp in scenarioFactories.OrderBy(kvp => kvp.Key))
                {
                    Console.WriteLine();
                    Console.WriteLine($"Executing scenario '{kvp.Key}'...");
                    try
                    {
                        var scenario = kvp.Value();
                        await scenario.ExecuteAsync(parsedArgs, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected exception: {ex}");
                        success = false;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"All scenarios completed {(success ? "successfully" : "with errors")}.");
                return success ? 0 : 1;
            }

            if (!scenarioFactories.TryGetValue(parsedArgs.Scenario, out var scenarioFactory))
            {
                Console.WriteLine($"Unknown scenario '{parsedArgs.Scenario}'. Supported values: ");
                foreach (var scenarioName in scenarioFactories.Keys.OrderBy(k => k))
                {
                    Console.WriteLine($"   {scenarioName}");
                }

                Console.WriteLine();
                return 1;
            }

            Console.WriteLine($"Executing scenario '{parsedArgs.Scenario}'.");
            try
            {
                var scenario = scenarioFactory();
                await scenario.ExecuteAsync(parsedArgs, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception: {ex}");
                return 1;
            }

            return 0;
        }
    }
}
