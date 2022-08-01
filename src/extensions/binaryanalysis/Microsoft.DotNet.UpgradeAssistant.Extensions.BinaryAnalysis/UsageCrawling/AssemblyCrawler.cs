// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class AssemblyCrawler
{
    private readonly Dictionary<ApiKey, int> _results = new();

    public CrawlerResults CreateResults()
    {
        return new CrawlerResults(_results);
    }

    public void Crawl(IAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        CrawlAttributes(assembly.Attributes);
        CrawlAttributes(assembly.ModuleAttributes);

        foreach (var type in assembly.GetAllTypes())
        {
            CrawlType(type);
        }
    }

    private void CrawlAttributes(IEnumerable<ICustomAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            Record(attribute.Type);
            Record(attribute.Constructor);

            var typeComponent = attribute.Type.UnWrap().UniqueId()[2..];

            foreach (var x in attribute.NamedArguments)
            {
                var fieldId = $"F:{typeComponent}.{x.ArgumentName.Value}";
                var propertyId = $"P:{typeComponent}.{x.ArgumentName.Value}";
                Record(fieldId);
                Record(propertyId);
            }
        }
    }

    private void CrawlType(ITypeDefinition type)
    {
        CrawlAttributes(type.Attributes);

        foreach (var b in type.BaseClasses)
        {
            Record(b);
        }

        foreach (var i in type.Interfaces)
        {
            Record(i);
        }

        foreach (var member in type.Members)
        {
            CrawlMember(member);
        }
    }

    private void CrawlMember(ITypeDefinitionMember member)
    {
        switch (member)
        {
            case ITypeDefinition type:
                CrawlType(type);
                break;
            case IMethodDefinition m:
                CrawlMethod(m);
                break;
            case IFieldDefinition f:
                CrawlField(f);
                break;
            case IPropertyDefinition p:
                CrawlProperty(p);
                break;
            case IEventDefinition e:
                CrawlEvent(e);
                break;
        }
    }

    private void CrawlMethod(IMethodDefinition m)
    {
        CrawlAttributes(m.Attributes);
        CrawlParameters(m.Parameters);
        Record(m.Type);

        foreach (var op in m.Body.Operations)
        {
            switch (op.Value)
            {
                case ITypeReference opT:
                    Record(opT);
                    break;
                case ITypeMemberReference opM:
                    Record(opM);
                    break;
            }
        }
    }

    private void CrawlParameters(IEnumerable<IParameterDefinition> parameters)
    {
        foreach (var parameter in parameters)
        {
            CrawlAttributes(parameter.Attributes);
            Record(parameter.Type);
        }
    }

    private void CrawlField(IFieldDefinition f)
    {
        CrawlAttributes(f.Attributes);
        Record(f.Type);
    }

    private void CrawlProperty(IPropertyDefinition p)
    {
        CrawlAttributes(p.Attributes);
        CrawlParameters(p.Parameters);
        Record(p.Type);
    }

    private void CrawlEvent(IEventDefinition e)
    {
        CrawlAttributes(e.Attributes);
        Record(e.Type);
    }

    private void Record(ITypeReference type)
    {
        if (type is IArrayTypeReference array)
        {
            Record(array.ElementType);
            return;
        }

        if (type is IGenericTypeInstanceReference generic)
        {
            foreach (var argument in generic.GenericArguments)
            {
                Record(argument);
            }
        }

        // We don't want tor record definitions
        var isDefinition = type.ResolvedType is not Dummy;
        if (isDefinition)
        {
            return;
        }

        var documentationId = type.UnWrap().UniqueId();
        Record(documentationId);
    }

    private void Record(ITypeMemberReference member)
    {
        // We don't want tor record definitions
        var isDefinition = member.ResolvedTypeDefinitionMember is not Dummy;
        if (isDefinition)
        {
            return;
        }

        if (member is ITypeReference type)
        {
            Record(type);
        }
        else
        {
            var documentationId = member.UnWrapMember().UniqueId();
            Record(documentationId);
        }
    }

    private void Record(string documentationId)
    {
        var key = new ApiKey(documentationId);
        _results.TryGetValue(key, out var count);
        _results[key] = count + 1;
    }
}
