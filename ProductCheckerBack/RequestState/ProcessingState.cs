using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.ExecutionState.DefaultStateHandler;

namespace ProductCheckerBack.ExecutionState
{
    internal class ProcessingState : IExecutionState
    {
        private readonly ArtemisDbContext _productCheckerDbContext;

        public ProcessingState(ArtemisDbContext productCheckerDbContext)
        {
            _productCheckerDbContext = productCheckerDbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor processingHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            processingHandlers.Process(_productCheckerDbContext, productCheckerService, new List<string>(), true);
        }
    }
}
