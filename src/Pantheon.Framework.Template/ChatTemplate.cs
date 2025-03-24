using System;
using System.Collections.Generic;
using Scriban;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Template
{
    /// <summary>
    /// A ChatML based template that can be rendered with input and parsed into output
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    /// <typeparam name="TOutput">The output type for the template</typeparam>
    public class ChatMlTemplate<TInput, TOutput> : IChatMlTemplate<TInput, TOutput> 
        where TInput : class 
        where TOutput : class
    {
        private readonly List<(string Role, Scriban.Template Template)> _messageTemplates;
        private readonly IOutputParser<TOutput> _outputParser;
        private readonly List<string>? _stopSequences;
        private readonly List<SafetySetting>? _safetySettings;

        /// <summary>
        /// Creates a new ChatML template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="messageTemplates">List of role and template content pairs</param>
        /// <param name="outputParser">The parser for the output</param>
        /// <param name="stopSequences">The stop sequences</param>
        /// <param name="safetySettings">The safety settings</param>
        public ChatMlTemplate(
            string name,
            List<(string Role, string Template)> messageTemplates,
            IOutputParser<TOutput> outputParser,
            List<string>? stopSequences = null,
            List<SafetySetting>? safetySettings = null)
        {
            Name = name;
            _messageTemplates = new List<(string Role, Scriban.Template Template)>();
            
            foreach (var (role, templateContent) in messageTemplates)
            {
                _messageTemplates.Add((role, Scriban.Template.Parse(templateContent)));
            }
            
            _outputParser = outputParser;
            _stopSequences = stopSequences;
            _safetySettings = safetySettings;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the stop sequences for the template
        /// </summary>
        public List<string>? StopSequences => _stopSequences;

        /// <summary>
        /// Gets the safety settings for the template
        /// </summary>
        public List<SafetySetting>? SafetySettings => _safetySettings;

        /// <inheritdoc />
        public List<ChatMlMessage> Render(TInput input)
        {
            var messages = new List<ChatMlMessage>();
            
            foreach (var (role, template) in _messageTemplates)
            {
                var context = new Scriban.TemplateContext();
                
                // Create a dictionary to hold the properties
                var scriptObject = new Scriban.Runtime.ScriptObject();
                
                // Add all properties of the input object to the context
                var type = input.GetType();
                foreach (var property in type.GetProperties())
                {
                    var value = property.GetValue(input);
                    scriptObject.Add(property.Name, value);
                }
                
                context.PushGlobal(scriptObject);

                string content = template.Render(context);
                messages.Add(new ChatMlMessage(role, content));
            }

            return messages;
        }

        /// <inheritdoc />
        public TOutput ParseResponse(string response)
        {
            return _outputParser.Parse(response);
        }
    }

    /// <summary>
    /// A ChatML streaming template that supports rendering a series of messages
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public class ChatMlStreamingTemplate<TInput> : IChatMlStreamingTemplate<TInput> where TInput : class
    {
        private readonly List<(string Role, Scriban.Template Template)> _messageTemplates;
        private readonly List<string>? _stopSequences;
        private readonly List<SafetySetting>? _safetySettings;

        /// <summary>
        /// Creates a new ChatML streaming template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="messageTemplates">List of role and template content pairs</param>
        /// <param name="stopSequences">The stop sequences</param>
        /// <param name="safetySettings">The safety settings</param>
        public ChatMlStreamingTemplate(
            string name,
            List<(string Role, string Template)> messageTemplates,
            List<string>? stopSequences = null,
            List<SafetySetting>? safetySettings = null)
        {
            Name = name;
            _messageTemplates = new List<(string Role, Scriban.Template Template)>();
            
            foreach (var (role, templateContent) in messageTemplates)
            {
                _messageTemplates.Add((role, Scriban.Template.Parse(templateContent)));
            }
            
            _stopSequences = stopSequences;
            _safetySettings = safetySettings;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the stop sequences for the template
        /// </summary>
        public List<string>? StopSequences => _stopSequences;

        /// <summary>
        /// Gets the safety settings for the template
        /// </summary>
        public List<SafetySetting>? SafetySettings => _safetySettings;

        /// <inheritdoc />
        public List<ChatMlMessage> Render(TInput input)
        {
            var messages = new List<ChatMlMessage>();
            
            foreach (var (role, template) in _messageTemplates)
            {
                var context = new Scriban.TemplateContext();
                
                // Create a dictionary to hold the properties
                var scriptObject = new Scriban.Runtime.ScriptObject();
                
                // Add all properties of the input object to the context
                var type = input.GetType();
                foreach (var property in type.GetProperties())
                {
                    var value = property.GetValue(input);
                    scriptObject.Add(property.Name, value);
                }
                
                context.PushGlobal(scriptObject);

                string content = template.Render(context);
                messages.Add(new ChatMlMessage(role, content));
            }

            return messages;
        }
    }

    /// <summary>
    /// A template for using LLM function calling capabilities
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public class ChatMlFunctionTemplate<TInput> : IChatMlFunctionTemplate<TInput> where TInput : class
    {
        private readonly List<(string Role, Scriban.Template Template)> _messageTemplates;
        private readonly List<LlmChatMlFunction> _functions;
        private readonly string? _functionCall;
        private readonly List<string>? _stopSequences;
        private readonly List<SafetySetting>? _safetySettings;

        /// <summary>
        /// Creates a new ChatML function template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="messageTemplates">List of role and template content pairs</param>
        /// <param name="functions">The functions to expose to the LLM</param>
        /// <param name="functionCall">Optional function to force calling</param>
        /// <param name="stopSequences">The stop sequences</param>
        /// <param name="safetySettings">The safety settings</param>
        public ChatMlFunctionTemplate(
            string name,
            List<(string Role, string Template)> messageTemplates,
            List<LlmChatMlFunction> functions,
            string? functionCall = null,
            List<string>? stopSequences = null,
            List<SafetySetting>? safetySettings = null)
        {
            Name = name;
            _messageTemplates = new List<(string Role, Scriban.Template Template)>();
            
            foreach (var (role, templateContent) in messageTemplates)
            {
                _messageTemplates.Add((role, Scriban.Template.Parse(templateContent)));
            }
            
            _functions = functions;
            _functionCall = functionCall;
            _stopSequences = stopSequences;
            _safetySettings = safetySettings;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the functions for the template
        /// </summary>
        public List<LlmChatMlFunction> Functions => _functions;

        /// <summary>
        /// Gets the function call specification
        /// </summary>
        public string? FunctionCall => _functionCall;

        /// <summary>
        /// Gets the stop sequences for the template
        /// </summary>
        public List<string>? StopSequences => _stopSequences;

        /// <summary>
        /// Gets the safety settings for the template
        /// </summary>
        public List<SafetySetting>? SafetySettings => _safetySettings;

        /// <inheritdoc />
        public List<ChatMlMessage> Render(TInput input)
        {
            var messages = new List<ChatMlMessage>();
            
            foreach (var (role, template) in _messageTemplates)
            {
                var context = new Scriban.TemplateContext();
                
                // Create a dictionary to hold the properties
                var scriptObject = new Scriban.Runtime.ScriptObject();
                
                // Add all properties of the input object to the context
                var type = input.GetType();
                foreach (var property in type.GetProperties())
                {
                    var value = property.GetValue(input);
                    scriptObject.Add(property.Name, value);
                }
                
                context.PushGlobal(scriptObject);

                string content = template.Render(context);
                messages.Add(new ChatMlMessage(role, content));
            }

            return messages;
        }

        /// <inheritdoc />
        public FunctionCallResult ParseResponse(LlmResponse response)
        {
            // Extract the function call details from the response
            return FunctionCallResult.FromResponse(response.Result);
        }
    }
}
