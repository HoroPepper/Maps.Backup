using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.WorkFlowEditor.CreateParam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.WorkFlowEditor.WorkNodes
{
    public class BaseWorkNode
    {
        public List<RefNodeParam> RefNodes { get; set; } = new List<RefNodeParam>();

        public List<RefDicVarParam> RefDicVars { get; set; } = new List<RefDicVarParam> { };

        public bool IsAllRefNodeSuccessed(Dictionary<string, TaskNodeResult> workResults)
        {

            if (RefNodes == null || !RefNodes.Any())
            {
                return true;
            }

            if (workResults == null || !workResults.Any())
            {
                return false;
            }

            foreach (var refNode in RefNodes)
            {
                string refNodeKey = refNode.RefNodeId;
                if (!workResults.ContainsKey(refNodeKey))
                {
                    return false;
                }
                string refNodeResultType = refNode.RefNodeResultType;
     
                if (!string.IsNullOrEmpty(refNodeResultType))
                {
                    bool isMatchType = false;
                    if (refNodeResultType == "file" && workResults[refNodeKey] is IFile)
                    {
                        isMatchType = true;
                    }
                    else if (refNodeResultType == "fileList" && workResults[refNodeKey] is IEnumerable<IFile>)
                    {
                        isMatchType = true;
                    }
                    else
                    {
                        isMatchType = false;
                    }

                    if(!isMatchType) 
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        public bool IsAllRefVarsExist(Dictionary<string, string> dicValues)
        {

            if (RefDicVars == null || !RefDicVars.Any())
            {
                return true;
            }

            if (dicValues == null || !dicValues.Any())
            {
                return false;
            }

            foreach (var refVar in RefDicVars)
            {
                string refVarKey = refVar.RefDicVarKey;
                if (!dicValues.ContainsKey(refVarKey))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
