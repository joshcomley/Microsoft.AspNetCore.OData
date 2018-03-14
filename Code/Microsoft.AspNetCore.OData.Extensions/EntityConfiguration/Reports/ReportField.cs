using System;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public class ReportField<TEntity> : IReportField
    {
        private Func<object, object> _baseFormatter;
        private Func<object, object> _baseNoValueFormatter;
        private Func<object, object> _baseCommentFormatter;
        private Expression<Func<TEntity, object>> _formatterExpression;
        private Expression<Func<TEntity, object>> _noValueFormatterExpression;
        private Expression<Func<TEntity, object>> _commentFormatterExpression;
        private Func<TEntity, string> _link;
        private Func<object, string> _baseLink;
        public string Title { get; set; }
        public string Key { get; set; }

        public Expression<Func<TEntity, object>> FormatterExpression
        {
            get => _formatterExpression;
            set
            {
                _formatterExpression = value;
                if (_formatterExpression != null)
                {
                    Formatter = _formatterExpression.Compile();
                }
                else
                {
                    Formatter = _ => "";
                }
                _baseFormatter = _ => Formatter((TEntity)_);
            }
        }

        public Expression<Func<TEntity, object>> CommentFormatterExpression
        {
            get => _commentFormatterExpression;
            set
            {
                _commentFormatterExpression = value;
                if (_commentFormatterExpression != null)
                {
                    CommentFormatter = _commentFormatterExpression.Compile();
                }
                else
                {
                    CommentFormatter = _ => "";
                }
                _baseCommentFormatter = _ => CommentFormatter((TEntity)_);
            }
        }


        public Expression<Func<TEntity, object>> NoValueFormatterExpression
        {
            get => _noValueFormatterExpression;
            set
            {
                _noValueFormatterExpression = value;
                if (_noValueFormatterExpression != null)
                {
                    NoValueFormatter = _noValueFormatterExpression.Compile();
                }
                else
                {
                    NoValueFormatter = _ => "";
                }
                _baseNoValueFormatter = _ => NoValueFormatter((TEntity)_);
            }
        }

        public ReportFieldKind Kind { get; set; }
        public ReportFieldStyle Style { get; set; }

        public Func<TEntity, string> Link
        {
            get => _link;
            set
            {
                _link = value;
                _baseLink = value == null ? (Func<object, string>)null : _ => value((TEntity)_);
            }
        }

        public Func<TEntity, object> CommentFormatter { get; private set; }
        public Func<TEntity, object> Formatter { get; private set; }
        public Func<TEntity, object> NoValueFormatter { get; private set; }

        Func<object, object> IReportField.CommentFormatter => _baseCommentFormatter;
        Func<object, object> IReportField.NoValueFormatter => _baseNoValueFormatter;
        Func<object, object> IReportField.Formatter => _baseFormatter;
        Func<object, string> IReportField.Link => _baseLink;

        public ReportField(
            string title, 
            Expression<Func<TEntity, object>> formatter,
            Expression<Func<TEntity, object>> noValueFormatter = null,
            Expression<Func<TEntity, object>> commentFormatter = null,
            ReportFieldKind kind = ReportFieldKind.String,
            ReportFieldStyle style = ReportFieldStyle.Normal,
            Func<TEntity, string> link = null,
            string key = null)
        {
            Title = title;
            FormatterExpression = formatter;
            NoValueFormatterExpression = noValueFormatter;
            CommentFormatterExpression = commentFormatter;
            Kind = kind;
            Style = style;
            Link = link;
            Key = key ?? title;
        }
    }
}