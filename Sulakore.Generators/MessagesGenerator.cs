﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sulakore.Generators
{
    [Generator]
    public class MessagesGenerator : ISourceGenerator
    {
        public record Message(bool IsOutgoing, string Name, string Hash, short Id);

        public void Execute(GeneratorExecutionContext context)
        {
            List<Message> outMessages = new(), inMessages = new();
            foreach ((bool IsOutgoing, string[] Items) in ParseEntries(context.AdditionalFiles.Single(at => at.Path.EndsWith("Messages.ini")).Path))
            {
                string name = Items[0], hash = null;
                if (!short.TryParse(Items[1], out short id))
                {
                    id = -1;
                    hash = Items[1];
                }
                else if (Items.Length > 2) hash = Items[2];
                (IsOutgoing ? outMessages : inMessages).Add(new Message(IsOutgoing, name, hash, id));
            }
            context.AddSource("Outgoing.cs", SourceText.From(CreateMessagesSource(outMessages, true), Encoding.UTF8));
            context.AddSource("Incoming.cs", SourceText.From(CreateMessagesSource(inMessages, false), Encoding.UTF8));
        }
        public void Initialize(GeneratorInitializationContext context)
        { }

        public static string CreateMessagesSource(IList<Message> messages, bool isOutgoing)
        {
            string className = (isOutgoing ? "Outgoing" : "Incoming");
            string isOutgoingString = isOutgoing.ToString().ToLowerInvariant();

            using var text = new StringWriter();
            using var indentedText = new IndentedTextWriter(text);

            indentedText.WriteLine("using System.Collections.Generic;");
            indentedText.WriteLine();
            indentedText.WriteLine("using Sulakore.Habbo.Web;");
            indentedText.WriteLine();

            indentedText.WriteLine("namespace Sulakore.Habbo.Messages");
            indentedText.WriteLine("{");
            indentedText.Indent++;

            indentedText.WriteLine($"public sealed class {className} : Identifiers");
            indentedText.WriteLine("{");
            indentedText.Indent++;

            indentedText.WriteLine($"public override int Count => {messages.Count};");
            indentedText.WriteLine($"public override bool IsOutgoing => {isOutgoingString};");
            indentedText.WriteLine();
            foreach (Message message in messages)
            {
                indentedText.WriteLine($"public HMessage {message.Name} {{ get; }}");
            }

            indentedText.WriteLine();
            indentedText.WriteLine($"public {className}() : this(null) {{ }}");
            indentedText.WriteLine($"public {className}(IHGame game) : base(game)");
            indentedText.WriteLine("{");
            indentedText.Indent++;

            foreach (Message message in messages)
            {
                indentedText.WriteLine($"{message.Name} = Initialize(\"{message.Name}\", {message.Id});");
            }

            indentedText.Indent--;
            indentedText.WriteLine("}");

            indentedText.Indent--;
            indentedText.WriteLine("}");

            indentedText.Indent--;
            indentedText.WriteLine("}");

            indentedText.Flush();
            return text.ToString();
        }
        public static IEnumerable<(bool IsOutgoing, string[] Items)> ParseEntries(string path)
        {
            bool isOutgoing = true;
            foreach (string line in File.ReadAllLines(path))
            {
                string[] items = line.Split(new char[] { '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = items[i].Trim();
                }

                if (items.Length == 1)
                {
                    if (line.Equals("[outgoing]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isOutgoing = true;
                    }
                    else if (line.Equals("[incoming]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isOutgoing = false;
                    }
                    continue;
                }
                yield return (isOutgoing, items);
            }
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}