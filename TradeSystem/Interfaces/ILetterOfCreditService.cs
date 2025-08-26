using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface ILetterOfCreditService
    {
        IEnumerable<LetterOfCredit> GetAll();
        LetterOfCredit? GetById(int id);
        bool CreateLetterOfCredit(LetterOfCredit lc);
        bool AmendLetterOfCredit(LetterOfCredit lc);
        bool CloseLetterOfCredit(int id);

    }
}
