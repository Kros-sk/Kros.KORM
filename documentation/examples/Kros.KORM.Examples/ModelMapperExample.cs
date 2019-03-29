namespace Kros.KORM.Examples
{
    internal class ModelMapperExample
    {
        public void SetColumnMapExample()
        {
            #region SetColumnName
            Database.DefaultModelMapper.SetColumnName<Person, string>(p => p.Name, "FirstName");
            #endregion
        }

        private class Person
        {
            public string Name { get; set; }
        }
    }
}
