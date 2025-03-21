using System;
using System.Collections.Generic;
using Scriban;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Template
{
    /// <summary>
    /// A template that can be rendered with input and parsed into output
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    /// <typeparam name="TOutput">The output type for the template</typeparam>
    public class Template<TInput, TOutput> : ITemplate<TInput, TOutput> 
        where TInput : class 
        where TOutput : class
    {
        private readonly Scriban.Template _template;
        private readonly Scriban.Template? _systemTemplate;
        private readonly IOutputParser<TOutput> _outputParser;
        private readonly List<string>? _stopSequences;
        private readonly List<SafetySetting>? _safetySettings;

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="templateContent">The template content</param>
        /// <param name="outputParser">The parser for the output</param>
        /// <param name="systemTemplateContent">The system template content</param>
        /// <param name="stopSequences">The stop sequences</param>
        /// <param name="safetySettings">The safety settings</param>
        public Template(
            string name,
            string templateContent,
            IOutputParser<TOutput> outputParser,
            string? systemTemplateContent = null,
            List<string>? stopSequences = null,
            List<SafetySetting>? safetySettings = null)
        {
            Name = name;
            _template = Scriban.Template.Parse(templateContent);
            _outputParser = outputParser;
            _stopSequences = stopSequences;
            _safetySettings = safetySettings;

            if (systemTemplateContent != null)
            {
                _systemTemplate = Scriban.Template.Parse(systemTemplateContent);
            }
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
        public string Render(TInput input)
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

            return _template.Render(context);
        }

        /// <inheritdoc />
        public string? RenderSystemTemplate(TInput input)
        {
            if (_systemTemplate == null)
            {
                return null;
            }

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

            return _systemTemplate.Render(context);
        }

        /// <inheritdoc />
        public TOutput ParseResponse(string response)
        {
            return _outputParser.Parse(response);
        }
    }

    /// <summary>
    /// A streaming template that supports rendering without parsing
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public class StreamingTemplate<TInput> : IStreamingTemplate<TInput> where TInput : class
    {
        private readonly Scriban.Template _template;
        private readonly Scriban.Template? _systemTemplate;
        private readonly List<string>? _stopSequences;
        private readonly List<SafetySetting>? _safetySettings;

        /// <summary>
        /// Creates a new streaming template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="templateContent">The template content</param>
        /// <param name="systemTemplateContent">The system template content</param>
        /// <param name="stopSequences">The stop sequences</param>
        /// <param name="safetySettings">The safety settings</param>
        public StreamingTemplate(
            string name,
            string templateContent,
            string? systemTemplateContent = null,
            List<string>? stopSequences = null,
            List<SafetySetting>? safetySettings = null)
        {
            Name = name;
            _template = Scriban.Template.Parse(templateContent);
            _stopSequences = stopSequences;
            _safetySettings = safetySettings;

            if (systemTemplateContent != null)
            {
                _systemTemplate = Scriban.Template.Parse(systemTemplateContent);
            }
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
        public string Render(TInput input)
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

            return _template.Render(context);
        }

        /// <inheritdoc />
        public string? RenderSystemTemplate(TInput input)
        {
            if (_systemTemplate == null)
            {
                return null;
            }

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

            return _systemTemplate.Render(context);
        }
    }

    /// <summary>
    /// Implementation of the output parser interface
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    public class OutputParser<TOutput> : IOutputParser<TOutput> where TOutput : class
    {
        private readonly Func<string, TOutput> _parseFunction;

        /// <summary>
        /// Creates a new output parser
        /// </summary>
        /// <param name="parseFunction">The function to parse the response</param>
        public OutputParser(Func<string, TOutput> parseFunction)
        {
            _parseFunction = parseFunction;
        }

        /// <inheritdoc />
        public TOutput Parse(string response)
        {
            return _parseFunction(response);
        }
    }
}
