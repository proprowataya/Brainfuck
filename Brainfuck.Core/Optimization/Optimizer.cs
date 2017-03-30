using Brainfuck.Core.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Brainfuck.Core.Optimization
{
    public partial class Optimizer
    {
        public Setting Setting { get; }

        public Optimizer(Setting setting)
        {
            Setting = setting;
        }

        public Module Optimize(Module module)
        {
            IReadOnlyList<IOperation> operations = module.Root.Operations;
            int ptrChange = module.Root.PtrChange;
            operations = OptimizeRoopStep(operations);
            operations = OptimizeReduceStep(operations);
            (operations, ptrChange) = OptimizePtrChangeStep(operations, ptrChange);
            return new Module(module.Source, new BlockUnitOperation(operations.ToImmutableArray(), ptrChange));
        }
    }
}
