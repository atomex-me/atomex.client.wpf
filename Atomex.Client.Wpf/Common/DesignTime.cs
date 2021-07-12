using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Configuration;

using Atomex.Abstract;
using Atomex.Common.Configuration;
using Atomex.Core;

namespace Atomex.Client.Wpf.Common
{
    public static class DesignTime
    {
        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

        private static readonly IConfiguration CurrenciesConfiguration = new ConfigurationBuilder()
            .AddEmbeddedJsonFile(CoreAssembly, "currencies.json")
            .Build()
            .GetSection(Network.TestNet.ToString());

        private static readonly IConfiguration SymbolsConfiguration = new ConfigurationBuilder()
            .AddEmbeddedJsonFile(CoreAssembly, "symbols.json")
            .Build()
            .GetSection(Network.TestNet.ToString());

        public static ICurrencies Currencies => new Currencies(CurrenciesConfiguration);
        public static ISymbols Symbols => new Symbols(SymbolsConfiguration);
    }
}