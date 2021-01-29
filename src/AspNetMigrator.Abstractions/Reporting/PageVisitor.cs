namespace AspNetMigrator.Reporting
{
    public abstract class PageVisitor
    {
        public abstract void Visit(Table table);

        public abstract void Visit(Divider divider);

        public abstract void Visit(Text text);

        public abstract void Visit(Section section);

        public void Visit(Page page)
        {
            if (page is null)
            {
                throw new System.ArgumentNullException(nameof(page));
            }

            var first = true;

            foreach (var content in page.Content)
            {
                if (!first)
                {
                    Visit(Divider.Instance);
                }
                else
                {
                    first = false;
                }

                Visit(content);
            }
        }

        public void Visit(Content content)
        {
            if (content is Table table)
            {
                Visit(table);
            }
            else if (content is Text text)
            {
                Visit(text);
            }
            else if (content is Section section)
            {
                Visit(section);
            }
        }
    }
}
