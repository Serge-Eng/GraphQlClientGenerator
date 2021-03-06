﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQlClientGenerator
{
    public delegate ScalarFieldTypeDescription GetCustomScalarFieldTypeDelegate(GraphQlType baseType, GraphQlTypeBase valueType, string valueName);
    
    public delegate string GetDataPropertyAccessorBodiesDelegate(string backingFieldName, ScalarFieldTypeDescription backingFieldType);

    public struct ScalarFieldTypeDescription
    {
        public string NetTypeName { get; set; }
        public string FormatMask { get; set; }
    }

    public class GraphQlGeneratorConfiguration
    {
        public CSharpVersion CSharpVersion { get; set; }

        public string ClassPostfix { get; set; }

        public IDictionary<string, string> CustomClassNameMapping { get; } = new Dictionary<string, string>();

        public CommentGenerationOption CommentGeneration { get; set; }

        public bool IncludeDeprecatedFields { get; set; }

        public bool GeneratePartialClasses { get; set; } = true;

        /// <summary>
        /// Determines whether unknown type scalar fields will be automatically requested when <code>WithAllScalarFields</code> issued.
        /// </summary>
        public bool TreatUnknownObjectAsScalar { get; set; }

        /// <summary>
        /// Determines the .NET type generated for GraphQL Integer data type.
        /// </summary>
        /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
        public IntegerTypeMapping IntegerTypeMapping { get; set; } = IntegerTypeMapping.Int32;

        /// <summary>
        /// Determines the .NET type generated for GraphQL Float data type.
        /// </summary>
        /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
        public FloatTypeMapping FloatTypeMapping { get; set; }

        /// <summary>
        /// Determines the .NET type generated for GraphQL Boolean data type.
        /// </summary>
        /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
        public BooleanTypeMapping BooleanTypeMapping { get; set; }

        /// <summary>
        /// Determines the .NET type generated for GraphQL ID data type.
        /// </summary>
        /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
        public IdTypeMapping IdTypeMapping { get; set; } = IdTypeMapping.Guid;
        
        public PropertyGenerationOption PropertyGeneration { get; set; } = PropertyGenerationOption.AutoProperty;

        public JsonPropertyGenerationOption JsonPropertyGeneration { get; set; } = JsonPropertyGenerationOption.CaseInsensitive;

        /// <summary>
        /// Determines builder class, data class and interfaces accessibility level.
        /// </summary>
        public MemberAccessibility MemberAccessibility { get; set; }

        /// <summary>
        /// This property is used for mapping GraphQL scalar type into specific .NET type. By default any custom GraphQL scalar type is mapped into <see cref="System.Object"/>.
        /// </summary>
        public GetCustomScalarFieldTypeDelegate CustomScalarFieldTypeMapping { get; set; }

        /// <summary>
        /// Used for custom data property accessor bodies generation; applicable only when <code>PropertyGeneration = PropertyGenerationOption.BackingField</code>.
        /// </summary>
        public GetDataPropertyAccessorBodiesDelegate PropertyAccessorBodyWriter { get; set; }

        public GraphQlGeneratorConfiguration() => Reset();

        public void Reset()
        {
            ClassPostfix = null;
            CustomClassNameMapping.Clear();
            CSharpVersion = CSharpVersion.Compatible;
            CustomScalarFieldTypeMapping = DefaultScalarFieldTypeMapping;
            PropertyAccessorBodyWriter = GeneratePropertyAccessors;
            CommentGeneration = CommentGenerationOption.Disabled;
            IncludeDeprecatedFields = false;
            FloatTypeMapping = FloatTypeMapping.Decimal;
            BooleanTypeMapping = BooleanTypeMapping.Boolean;
            IntegerTypeMapping = IntegerTypeMapping.Int32;
            IdTypeMapping = IdTypeMapping.Guid;
            TreatUnknownObjectAsScalar = false;
            GeneratePartialClasses = true;
            MemberAccessibility = MemberAccessibility.Public;
            JsonPropertyGeneration = JsonPropertyGenerationOption.CaseInsensitive;
            PropertyGeneration = PropertyGenerationOption.AutoProperty;
        }

        public ScalarFieldTypeDescription DefaultScalarFieldTypeMapping(GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
        {
            valueName = NamingHelper.ToPascalCase(valueName);

            if (valueName == "From" || valueName == "ValidFrom" || valueName == "To" || valueName == "ValidTo" ||
                valueName == "CreatedAt" || valueName == "UpdatedAt" || valueName == "ModifiedAt" || valueName == "DeletedAt" ||
                valueName.EndsWith("Timestamp"))
                return new ScalarFieldTypeDescription { NetTypeName = "DateTimeOffset?" };

            valueType = (valueType as GraphQlFieldType)?.UnwrapIfNonNull() ?? valueType;
            if (valueType.Kind == GraphQlTypeKind.Enum)
                return new ScalarFieldTypeDescription { NetTypeName = NamingHelper.ToPascalCase(valueType.Name) + "?" };

            var dataType = valueType.Name == GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
            return new ScalarFieldTypeDescription { NetTypeName = GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(this, dataType) };
        }

        public string GeneratePropertyAccessors(string backingFieldName, ScalarFieldTypeDescription backingFieldType)
        {
            var useCompatibleVersion = CSharpVersion == CSharpVersion.Compatible;
            var builder = new StringBuilder();
            builder.Append(" { get");
            builder.Append(useCompatibleVersion ? " { return " : " => ");
            builder.Append(backingFieldName);
            builder.Append(";");

            if (useCompatibleVersion)
                builder.Append(" }");

            builder.Append(" set");
            builder.Append(useCompatibleVersion ? " { " : " => ");
            builder.Append(backingFieldName);
            builder.Append(" = value;");

            if (useCompatibleVersion)
                builder.Append(" }");

            builder.Append(" }");

            return builder.ToString();
        }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest,
        NewestWithNullableReferences
    }

    public enum FloatTypeMapping
    {
        Decimal,
        Float,
        Double,
        Custom
    }

    public enum BooleanTypeMapping
    {
        Boolean,
        Custom
    }

    public enum IntegerTypeMapping
    {
        Int16,
        Int32,
        Int64,
        Custom
    }

    public enum IdTypeMapping
    {
        String,
        Guid,
        Object,
        Custom
    }

    public enum MemberAccessibility
    {
        Public,
        Internal
    }

    public enum JsonPropertyGenerationOption
    {
        Never,
        Always,
        UseDefaultAlias,
        CaseInsensitive,
        CaseSensitive
    }

    public enum PropertyGenerationOption
    {
        AutoProperty,
        BackingField
    }

    [Flags]
    public enum CommentGenerationOption
    {
        Disabled = 0,
        CodeSummary = 1,
        DescriptionAttribute = 2
    }
}