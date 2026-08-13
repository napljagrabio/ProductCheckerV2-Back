using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.ExecutionState.DefaultStateHandler;

namespace ProductCheckerBack.ExecutionState
{
    internal class SuccessState : IExecutionState
    {
        private readonly ArtemisDbContext _productCheckerDbContext;

        public SuccessState(ArtemisDbContext productCheckerDbContext)
        {
            _productCheckerDbContext = productCheckerDbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor successHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            successHandlers.Process(_productCheckerDbContext, productCheckerService, new List<string>());
        }
    }
}