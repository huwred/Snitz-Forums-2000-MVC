namespace SnitzDataModel.Models
{
    public partial class ForumTotals
    {
        public static void AddUser()
        {
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "TOTALS SET U_COUNT = U_COUNT + 1");
        }
        public static void DeleteUser()
        {
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "TOTALS SET U_COUNT = U_COUNT - 1");
        }
    }
}