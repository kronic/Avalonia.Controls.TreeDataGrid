using System;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    public interface ISearchableTemplateColumn<TModel> : ITextSearchable
    {
        public Func<TModel,string?>? TextSearchValueSelector { get; set; }
    }
}
