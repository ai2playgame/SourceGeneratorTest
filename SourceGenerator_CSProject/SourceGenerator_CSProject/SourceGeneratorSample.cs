using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator_CSProject
{
    [Generator]
    public class AutoPropertyGenerator : ISourceGenerator
    {
        private const string AttributeText = @"
using System;
namespace AutoProperty
{
    [AttributeUsage(AttributeTargets.Field,
        Inherited = false, AllowMultiple = false)]
    sealed class AutoPropertyAttribute : Attribute
    {
        public AutoPropertyAttribute()
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // 1. コード中のフィールド一覧を収集するSyntaxReceiverをここで登録する
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // AutoProperty.Generated.csという名前で、AutoPropertyAttributeクラスを実装するソースコードを生成する
            context.AddSource("AutoProperty.Generated.cs", SourceText.From(AttributeText, Encoding.UTF8));

            // context.SyntaxReceiverに登録したSyntaxReceiverが格納されている
            var receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null) return;

            // SyntaxReceiverが収集したフィールド一覧の内、AutoProperty属性がついたものを抽出する
            var fieldSymbols = new List<IFieldSymbol>();
            foreach (var field in receiver.TargetFields)
            {
                var model = context.Compilation.GetSemanticModel(field.SyntaxTree);
                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    // フィールドの属性から、AutoProperty属性があるかを確認
                    var attribute = fieldSymbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass.Name == "AutoPropertyAttribute");
                    if (attribute != null)
                    {
                        // あったら追加
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }
        
            // 3. AutoProperty属性がついたフィールドに対して、プロパティを生成する（プロパティはpartialクラス経由で追加する）
        
            // クラス単位にまとめて、そこからpartialなクラスを生成したい
            foreach (var group in fieldSymbols.GroupBy(field => field.ContainingType))
            {
                var classSymbol = group.Key;
                var fieldSymbolList = group.ToList();
            
                // classSourceにクラス定義のコードが入る
                var classSource = ProcessClass(classSymbol, fieldSymbolList);
                // クラス名.Generated.csという名前でコード生成
                context.AddSource($"{classSymbol.Name}.Generated.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fieldSymbols)
        {
            var builder = new StringBuilder($@"
namespace {classSymbol.ContainingNamespace.ToDisplayString()}
{{
    public partial class {classSymbol.Name}
    {{
");
            foreach (var fieldSymbol in fieldSymbols)
            {
                // フィールド定義ごとに対応するプロパティを生成
                var fieldTypeName = fieldSymbol.Type.ToDisplayString();
                var originalFieldName = fieldSymbol.Name;
                var propertyName = char.ToUpper(originalFieldName[0]) + originalFieldName.Substring(1);

                builder.Append($@"
        public {fieldTypeName} {propertyName}
        {{
            get {{ return this.{originalFieldName}; }}
            set {{ this.{originalFieldName} = value; }}
        }}
");
            }

            builder.Append($@"
    }}
}}
");
            
            return builder.ToString();
        }
    }
}