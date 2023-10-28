using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;

namespace CoreLib.Editor
{
    public class HeadingUpperMarkdownExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var headingBlockParser = pipeline.BlockParsers.Find<HeadingBlockParser>();
            if (headingBlockParser != null)
            {
                // Install a hook on the HeadingBlockParser when a HeadingBlock is actually processed
                headingBlockParser.TryParseAttributes -= TryParseAttributes;
                headingBlockParser.TryParseAttributes += TryParseAttributes;
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private bool TryParseAttributes(BlockProcessor processor, ref StringSlice slice, IBlock block)
        {
            if (block is HeadingBlock headingBlock)
            {
                headingBlock.Level++;
            }

            return true;
        }
    }
    
    public static class MarkdownExtensions{
    
        public static MarkdownPipelineBuilder UseHeadingUpper(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<HeadingUpperMarkdownExtension>();
            return pipeline;
        }
    }
}