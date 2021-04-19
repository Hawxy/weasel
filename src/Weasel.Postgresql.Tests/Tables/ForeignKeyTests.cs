using Shouldly;
using Weasel.Postgresql.Tables;
using Xunit;

namespace Weasel.Postgresql.Tests.Tables
{
    public class ForeignKeyTests
    {
        [Fact]
        public void write_fk_ddl()
        {
            var table = new Table("people");
            var fk = new ForeignKey("fk_state")
            {
                LinkedTable = new DbObjectName("states"),
                ColumnNames = new []{"state_id"},
                LinkedNames = new []{"id"}
            };

            var ddl = fk.ToDDL(table);
            ddl.ShouldNotContain("ON DELETE");
            ddl.ShouldNotContain("ON UPDATE");

            ddl.ShouldContain("ALTER TABLE public.people");
            ddl.ShouldContain("ADD CONSTRAINT fk_state FOREIGN KEY(state_id)");
            ddl.ShouldContain("REFERENCES public.states(id)");
        }
        
        [Fact]
        public void write_fk_ddl_with_on_delete()
        {
            var table = new Table("people");
            var fk = new ForeignKey("fk_state")
            {
                LinkedTable = new DbObjectName("states"),
                ColumnNames = new []{"state_id"},
                LinkedNames = new []{"id"},
                OnDelete = CascadeAction.Restrict
            };
            
            

            var ddl = fk.ToDDL(table);
            ddl.ShouldContain("ON DELETE RESTRICT");
            ddl.ShouldNotContain("ON UPDATE");
        }
        
        [Fact]
        public void write_fk_ddl_with_on_update()
        {
            var table = new Table("people");
            var fk = new ForeignKey("fk_state")
            {
                LinkedTable = new DbObjectName("states"),
                ColumnNames = new []{"state_id"},
                LinkedNames = new []{"id"},
                OnUpdate = CascadeAction.Cascade
            };
            
            

            var ddl = fk.ToDDL(table);
            ddl.ShouldNotContain("ON DELETE");
            ddl.ShouldContain("ON UPDATE CASCADE");
        }
        
        [Fact]
        public void write_fk_ddl_with_multiple_columns()
        {
            var table = new Table("people");
            var fk = new ForeignKey("fk_state")
            {
                LinkedTable = new DbObjectName("states"),
                ColumnNames = new []{"state_id", "tenant_id"},
                LinkedNames = new []{"id", "tenant_id"}
            };

            var ddl = fk.ToDDL(table);


            ddl.ShouldContain("ALTER TABLE public.people");
            ddl.ShouldContain("ADD CONSTRAINT fk_state FOREIGN KEY(state_id, tenant_id)");
            ddl.ShouldContain("REFERENCES public.states(id, tenant_id)");
        }
    }
}