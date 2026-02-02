using Maps.Backup.Core.Interfaces;
using Maps.Backup.WorkFlowEditor.CreateParam;
using Maps.Backup.WorkFlowEditor.WorkNodes;

namespace Maps.Backup.WorkFlowEditor
{
    public class WorkNodeFactory
    {
        public IWorkTaskNode CreateNode(WorkNodeCreateParam workNodeCreateParam)
        {
            if(workNodeCreateParam == null)
            {
                return new DelegateNode();
            }

            return new DelegateNode();
        }
    }
}
