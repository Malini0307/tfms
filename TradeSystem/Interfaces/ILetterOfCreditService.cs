using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface ILetterOfCreditService
    {
        IEnumerable<LetterOfCredit> GetAll();
        IEnumerable<LetterOfCredit> GetByUserId(string userId);
        LetterOfCredit? GetById(int id);
        LetterOfCredit? GetByIdAndUserId(int id, string userId);
        bool CreateLetterOfCredit(LetterOfCredit lc, string userId);
        bool AmendLetterOfCredit(LetterOfCredit lc, string userId);
        bool CloseLetterOfCredit(int id);

    }
}
