using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharphound.Client;
using SharpHoundCommonLib;
using SharpHoundCommonLib.Enums;
using SharpHoundCommonLib.LDAPQueries;
using SharpHoundCommonLib.OutputTypes;

namespace Sharphound.Producers
{

    internal class StealthProducer : BaseProducer
    {
        private static bool _stealthTargetsBuilt;
        private readonly string[] _props;
        private readonly string[] _propsConfigNC;
        private readonly LdapFilter _query;
        private readonly LdapFilter _queryConfigNC;

        public StealthProducer(IContext context, Channel<IDirectoryObject> channel, Channel<OutputBase> outputChannel) : base(context, channel, outputChannel)
        {
            var ldapData = CreateDefaultNCData();
            _query = ldapData.Filter;
            _props = ldapData.Attributes;

            var configNCData = CreateConfigNCData();
            _queryConfigNC = configNCData.Filter;
            _propsConfigNC = configNCData.Attributes;
        }


        public override async Task Produce()
        {
            var cancellationToken = Context.CancellationTokenSource.Token;
            if (!_stealthTargetsBuilt)
                BuildStealthTargets();

            await foreach (var result in Context.LDAPUtils.PagedQuery(new LdapQueryParameters() {
                         LDAPFilter = _query.GetFilter(),
                         Attributes = _props,
                         SearchBase = Context.SearchBase,
                         DomainName = Context.DomainName
                     }, cancellationToken)) {
                if (!result.IsSuccess) {
                    Context.Logger.LogError("Error in stealth producer: {Message} ({Code})", result.Error, result.ErrorCode);
                    break;
                }
                await Channel.Writer.WriteAsync(result.Value, cancellationToken);
            }
        }

        public override async Task ProduceConfigNC()
        {
            if (string.IsNullOrEmpty(_queryConfigNC.GetFilter()))
                return;
            var cancellationToken = Context.CancellationTokenSource.Token;
            if (!_stealthTargetsBuilt)
                BuildStealthTargets();
            
            await foreach (var result in Context.LDAPUtils.PagedQuery(new LdapQueryParameters() {
                               LDAPFilter = _query.GetFilter(),
                               Attributes = _props,
                               DomainName = Context.DomainName,
                               NamingContext = NamingContext.Configuration
                           }, cancellationToken)) {
                if (!result.IsSuccess) {
                    Context.Logger.LogError("Error in stealth producer: {Message} ({Code})", result.Error, result.ErrorCode);
                    break;
                }
                await Channel.Writer.WriteAsync(result.Value, cancellationToken);
            }

        }

        private async void BuildStealthTargets()
        {
            Context.Logger.LogInformation("Finding Stealth Targets from LDAP Properties");

            var targets = await FindPathTargetSids();
            if (!Context.Flags.ExcludeDomainControllers) targets.Merge(await FindDomainControllers());

            StealthContext.AddStealthTargetSids(targets);
            _stealthTargetsBuilt = true;
        }

        private async Task<Dictionary<string, IDirectoryObject>> FindDomainControllers() {
            var res = new Dictionary<string, IDirectoryObject>();
            await foreach (var result in Context.LDAPUtils.PagedQuery(new LdapQueryParameters() {
                               LDAPFilter = CommonFilters.DomainControllers,
                               Attributes = _props,
                               DomainName = Context.DomainName
                           })) {
                if (!result.IsSuccess) {
                    break;
                }

                if (!result.Value.TryGetSecurityIdentifier(out var sid)) {
                    continue;
                }
                
                res.Add(sid, result.Value);
            }

            return res;
        }

        private async Task<Dictionary<string, IDirectoryObject>> FindPathTargetSids()
        {
            var paths = new ConcurrentDictionary<string, byte>();
            var sids = new Dictionary<string, IDirectoryObject>();
            
            var query = new LdapFilter();
            query.AddComputers("(|(homedirectory=*)(scriptpath=*)(profilepath=*))");
            foreach (var domain in Context.Domains)
            {
                await foreach (var searchResult in Context.LDAPUtils.PagedQuery(new LdapQueryParameters() {
                                   LDAPFilter = query.GetFilter(),
                                   Attributes = CommonProperties.StealthProperties,
                                   DomainName = domain.Name
                               })) {
                    if (searchResult.IsSuccess) {
                        var poss = new[]
                        {
                            searchResult.Value.GetProperty("homedirectory"), searchResult.Value.GetProperty("scriptpath"),
                            searchResult.Value.GetProperty("profilepath")
                        }.Where(s => s != null);
                        
                        foreach (var s in poss)
                        {
                            var split = s.Split('\\');
                            if (!(split.Length >= 3)) continue;
                            var path = split[2];
                            paths.TryAdd(path, new byte());
                        }
                    }
                }
            }
            
            foreach (var path in paths.Keys)
            {
                if (await Context.LDAPUtils.ResolveHostToSid(path, Context.DomainName) is (true, var sid)) {
                    if (sid != null && sid.StartsWith("S-1-5")) {
                        var searchResult = await Context.LDAPUtils.Query(new LdapQueryParameters() {
                            LDAPFilter = CommonFilters.SpecificSID(sid),
                            SearchScope = SearchScope.Subtree,
                            Attributes = _props,
                            DomainName = Context.DomainName
                        }).FirstOrDefaultAsync(LdapResult<IDirectoryObject>.Fail());

                        if (searchResult.IsSuccess) {
                            sids.Add(sid, searchResult.Value);    
                        }
                    }
                }
            }

            return sids;
        }
    }
}