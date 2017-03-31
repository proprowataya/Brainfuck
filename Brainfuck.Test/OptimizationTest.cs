using Brainfuck.Core;
using Brainfuck.Core.Analysis;
using Brainfuck.Core.LowLevel;
using Brainfuck.Core.Optimization;
using System.Linq;
using Xunit;

namespace Brainfuck.Test
{
    public class OptimizationTest
    {
        [Fact]
        public void DoubleOptimizationTest()
        {
            const string Code = ">+++++++++[<++++++++>-]<.>+++++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]++++++++++.";
            var module = Parser.Parse(Code);
            var optimized = new Optimizer(Setting.Default).Optimize(module);
            var doubleOptimized = new Optimizer(Setting.Default).Optimize(optimized);
            Assert.Equal(
                optimized.Root.ToLowLevel(Setting.Default.WithUseDynamicBuffer(false)).AsEnumerable(),
                doubleOptimized.Root.ToLowLevel(Setting.Default.WithUseDynamicBuffer(false)).AsEnumerable());
            Assert.Equal(
                optimized.Root.ToLowLevel(Setting.Default.WithUseDynamicBuffer(true)).AsEnumerable(),
                doubleOptimized.Root.ToLowLevel(Setting.Default.WithUseDynamicBuffer(true)).AsEnumerable());
        }
    }
}
