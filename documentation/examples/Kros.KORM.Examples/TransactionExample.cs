using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kros.KORM.Examples.ExecuteStoredProcedureExamples;

namespace Kros.KORM.Examples
{
    internal class TransactionExample
    {
        private void Transactions()
        {
            using (var database = new Database("", ""))
            {
                var invoices = new List<Invoice>();
                var items = new List<Item>();
                #region Transaction
                using (var transaction = database.BeginTransaction())
                {
                    var invoicesDbSet = database.Query<Invoice>().AsDbSet();
                    var itemsDbSet = database.Query<Item>().AsDbSet();

                    try
                    {
                        invoicesDbSet.Add(invoices);
                        invoicesDbSet.CommitChanges();

                        itemsDbSet.Add(items);
                        itemsDbSet.CommitChanges();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
                #endregion
            }
        }

        private void TransactionsIsolationLevel()
        {
            using (var database = new Database("", ""))
            {
                var invoices = new List<Invoice>();
                var items = new List<Item>();
                #region TransactionsIsolationLevel
                using (var transaction = database.BeginTransaction(IsolationLevel.Chaos))
                {
                    var invoicesDbSet = database.Query<Invoice>().AsDbSet();
                    var itemsDbSet = database.Query<Item>().AsDbSet();

                    try
                    {
                        invoicesDbSet.Add(invoices);
                        invoicesDbSet.CommitChanges();

                        itemsDbSet.Add(items);
                        itemsDbSet.CommitChanges();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
                #endregion
            }
        }

        private void TransactionsCommandTimeout()
        {
            using (var database = new Database("", ""))
            {
                #region TransactionCommandTimeout
                IEnumerable<Person> persons = null;

                using (var transaction = database.BeginTransaction(IsolationLevel.Chaos))
                {
                    transaction.CommandTimeout = 150;

                    try
                    {
                        persons = database.ExecuteStoredProcedure<IEnumerable<Person>>("LongRunningProcedure_GetPersons");
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
                #endregion
            }
        }

        public class Invoice
        {
        }
        public class Item
        {
        }
    }
}
