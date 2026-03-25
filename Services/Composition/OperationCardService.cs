using CompositionMaster.Models;
using CompositionMaster.Services.Base;
using CompositionMaster.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.Composition
{
    public class OperationCardService : BaseService
    {
        private readonly UserHelper _userHelper;

        public OperationCardService(ApplicationContext context, UserHelper userHelper) : base(context)
        {
            _userHelper = userHelper;
        }

        public List<OperationCard> GetOperationCards(int specificationId)
        {
            return _context.OperationCards
                .Where(oc => oc.Identifier == specificationId)
                .OrderBy(oc => oc.LineNumber)
                .ToList();
        }

        public OperationCard? GetOperationCard(int specificationId, int lineNumber)
        {
            return _context.OperationCards
                .FirstOrDefault(oc => oc.Identifier == specificationId && oc.LineNumber == lineNumber);
        }

        public bool OperationCardLineExists(int specificationId, int lineNumber)
        {
            return _context.OperationCards
                .Any(oc => oc.Identifier == specificationId && oc.LineNumber == lineNumber);
        }

        public void CreateOperationCard(int specificationId, OperationCard card, HttpContext httpContext)
        {
            Create(card);
            
            var change = new OperationCardChange
            {
                Identifier = card.Identifier,
                LineNumber = card.LineNumber,
                Department = card.Department,
                Section = card.Section,
                Operation = card.Operation,
                Equipment = card.Equipment,
                TimeNorm = card.TimeNorm,
                Tariff = card.Tariff,
                Cost = card.Cost,
                Sum = card.Sum,
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public void UpdateOperationCard(int specificationId, int lineNumber, OperationCard card, HttpContext httpContext)
        {
            var oldVersion = GetOperationCard(specificationId, lineNumber);
            if (oldVersion == null) return;
            
            Update(card);
            
            var change = new OperationCardChange
            {
                Identifier = card.Identifier,
                LineNumber = card.LineNumber,
                Department = card.Department,
                Section = card.Section,
                Operation = card.Operation,
                Equipment = card.Equipment,
                TimeNorm = card.TimeNorm,
                Tariff = card.Tariff,
                Cost = card.Cost,
                Sum = card.Sum,
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public bool DeleteOperationCard(int specificationId, int lineNumber, HttpContext httpContext)
        {
            var card = GetOperationCard(specificationId, lineNumber);
            if (card == null) return false;
            
            var change = new OperationCardChange
            {
                Identifier = card.Identifier,
                LineNumber = card.LineNumber,
                Department = card.Department,
                Section = card.Section,
                Operation = card.Operation,
                Equipment = card.Equipment,
                TimeNorm = card.TimeNorm,
                Tariff = card.Tariff,
                Cost = card.Cost,
                Sum = card.Sum,
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
            
            return Delete<OperationCard>(specificationId, lineNumber);
        }

        public void CreateOperationCardsBatch(int specificationId, List<OperationCard> cards, HttpContext httpContext)
        {
            foreach (var card in cards)
            {
                Create(card);
                
                var change = new OperationCardChange
                {
                    Identifier = card.Identifier,
                    LineNumber = card.LineNumber,
                    Department = card.Department,
                    Section = card.Section,
                    Operation = card.Operation,
                    Equipment = card.Equipment,
                    TimeNorm = card.TimeNorm,
                    Tariff = card.Tariff,
                    Cost = card.Cost,
                    Sum = card.Sum,
                    Author = _userHelper.GetCurrentUserId(httpContext)
                };
                Create(change);
            }
        }

        public List<OperationCardChange> GetOperationCardHistory(int identifier, int lineNumber)
        {
            return _context.OperationCardChanges
                .Where(occ => occ.Identifier == identifier && occ.LineNumber == lineNumber)
                .ToList();
        }
    }
}