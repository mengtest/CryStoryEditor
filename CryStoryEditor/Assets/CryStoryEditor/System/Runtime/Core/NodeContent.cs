﻿/**********************************************************
*Author: wangjiaying
*Date: 2016.7.7
*Func:
**********************************************************/
using System.Collections.Generic;
using System.IO;

namespace CryStory.Runtime
{

    public class NodeContent : UpdateNode
    {
        /// <summary>
        /// 存储了位于该容器中所有一级节点
        /// </summary>
        protected List<NodeModifier> _contenNodeList = new List<NodeModifier>(2);

        /// <summary>
        /// 运行时保存将要删除者
        /// </summary>
        private List<NodeModifier> _toRemoveNode = new List<NodeModifier>();
        /// <summary>
        /// 运行时保存将要添加者
        /// </summary>
        private List<NodeModifier> _toAddNode = new List<NodeModifier>();

        /// <summary>
        /// 获取所有处于该容器的一级节点
        /// </summary>
        public NodeModifier[] Nodes { get { return _contenNodeList.ToArray(); } }

        /// <summary>
        /// 缓存，以备结束时恢复初始容器节点
        /// </summary>
        protected NodeModifier[] _tempNodeList = null;

        /// <summary>
        /// 当Content中的节点加载完毕
        /// </summary>
        public event System.Action<NodeContent> OnNodeLoaded;

        /// <summary>
        /// 将节点添加至该容器。注意：不会更改节点本身容器数据！
        /// 使用节点SetContent代替
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool AddContentNode(NodeModifier node)
        {
            if (_contenNodeList.Contains(node) || node == null) return false;
            //不允许已运行节点的子节点添加
            //for (int i = 0; i < _contenNodeList.Count; i++)
            //{
            //    if (_contenNodeList[i].IsChild(node)) return false;
            //}
            _contenNodeList.Add(node);
            OnAddedContentNode(node);
            //node.SetContent(this);
            return true;
        }

        /// <summary>
        /// 若为容器，获取该容器中指定ID节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public NodeModifier GetNodeByID(int id)
        {
            for (int i = 0; i < _contenNodeList.Count; i++)
            {
                if (_contenNodeList[i]._id == id) return _contenNodeList[i];
                NodeModifier node = _contenNodeList[i].GetNextNodeByID(id);
                if (node != null) return node;
            }
            return null;
        }

        /// <summary>
        /// 将一组节点添加至容器
        /// </summary>
        /// <param name="nodes"></param>
        public void AddContentNode(NodeModifier[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                AddContentNode(nodes[i]);
            }
        }

        /// <summary>
        /// 将一组节点添加至容器
        /// </summary>
        /// <param name="nodes"></param>
        public void AddContentNode(List<NodeModifier> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                AddContentNode(nodes[i]);
            }
        }

        /// <summary>
        /// 删除一个节点
        /// </summary>
        public bool RemoveContenNode(NodeModifier node)
        {
            return _contenNodeList.Remove(node);
        }

        protected override EnumResult OnStart()
        {
            _tempNodeList = _contenNodeList.ToArray();
            return base.OnStart();
        }

        protected override EnumResult OnUpdate()
        {
            if (_contenNodeList.Count == 0) return EnumResult.Success;
            //Run
            for (int i = 0; i < _contenNodeList.Count; i++)
            {
                if (i >= _contenNodeList.Count) break;
                EnumResult result = _contenNodeList[i].Tick(this);

                if (result != EnumResult.Running)
                {
                    if (i >= _contenNodeList.Count) break;
                    NodeModifier node = _contenNodeList[i];

                    if (result == EnumResult.Failed)
                        switch (node.RunMode)
                        {
                            case EnumRunMode.UntilSuccess:
                                continue;
                            case EnumRunMode.ReturnParentNode:
                                _toRemoveNode.Add(node);
                                _toAddNode.Add(node.Parent);
                                continue;
                            case EnumRunMode.StopNodeList:
                                _toRemoveNode.Add(node);
                                continue;
                        }

                    _toRemoveNode.Add(node);

                    //单独处理Decorator节点运行
                    //仅运行非单节点
                    //if (node is Decorator)
                    //{
                    //    NodeModifier child = System.Array.Find<NodeModifier>(node.NextNodes, (n) => n.Parent == node);
                    //    if (child != null)
                    //        _toAddNode.Add(child);
                    //}
                    //else
                    node.GetNextNodes(_toAddNode);
                }
            }
            //UnityEngine.Profiler.EndSample();

            ProcessNode();

            return EnumResult.Running;
        }

        /// <summary>
        /// 处理容器类节点运行中的增删情况
        /// </summary>
        protected void ProcessNode()
        {
            //Remove
            if (_toRemoveNode.Count > 0)
            {
                for (int i = 0; i < _toRemoveNode.Count; i++)
                {
                    _contenNodeList.Remove(_toRemoveNode[i]);
                }
                _toRemoveNode.Clear();
            }

            //Add
            if (_toAddNode.Count > 0)
            {
                AddContentNode(_toAddNode);
                _toAddNode.Clear();
            }
        }

        /// <summary>
        /// 结束之后，恢复节点
        /// </summary>
        protected override void OnEnd()
        {
            base.OnEnd();

            _contenNodeList.Clear();
            _contenNodeList.AddRange(_tempNodeList);
            _tempNodeList = null;
        }

        protected virtual void OnAddedContentNode(NodeModifier node) { }

        //Save 
        protected override void OnSaved(BinaryWriter w)
        {
            base.OnSaved(w);

            bool running = _running;//_tempNodeList != null;
            //if (running) running = _tempNodeList.Length > 0;

            w.Write(running);
            if (running)
            {
                //Save Oringin Node
                //处于正在运行节点，保存初始节点及当前运行节点ID
                w.Write(_tempNodeList.Length);
                for (int i = 0; i < _tempNodeList.Length; i++)
                {
                    NodeModifier node = _tempNodeList[i];
                    System.Type type = node.GetType();

                    w.Write(type.FullName);
                    node.Serialize(w);
                }

                w.Write(_contenNodeList.Count);
                for (int i = 0; i < _contenNodeList.Count; i++)
                {
                    w.Write(_contenNodeList[i]._id);
                }
            }
            else {
                //未运行节点，直接保存当前节点即可
                w.Write(_contenNodeList.Count);
                for (int i = 0; i < _contenNodeList.Count; i++)
                {
                    NodeModifier node = _contenNodeList[i];
                    System.Type type = node.GetType();

                    w.Write(type.FullName);
                    node.Serialize(w);
                }
            }

        }

        protected override void OnLoaded(BinaryReader r)
        {
            //清空当前容器节点，避免被重复添加
            _contenNodeList.Clear();

            base.OnLoaded(r);
            bool running = r.ReadBoolean();

            //恢复初始节点
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string fullName = r.ReadString();
                NodeModifier node = ReflectionHelper.CreateInstance<NodeModifier>(fullName);
                if (node == null)
                {
                    node = ReflectionHelper.CreateInstance<NodeModifier>("CryStory.Runtime.MissingNode");
                    //It's mission
                    //return;
                }
                node.SetContent(this);
                node.Deserialize(r);
                NodeModifier.SetContent(node, this);
            }

            if (running)
            {
                //恢复运行节点（以ID为主）
                count = r.ReadInt32();
                List<NodeModifier> runningNode = new List<NodeModifier>();
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < _contenNodeList.Count; j++)
                    {
                        NodeModifier node = _contenNodeList[j].GetNextNodeByID(r.ReadInt32());
                        if (node != null)
                        {
                            runningNode.Add(node);
                            break;
                        }
                    }
                }

                //装填缓存节点
                _tempNodeList = _contenNodeList.ToArray();

                //移除初始节点
                for (int i = 0; i < _tempNodeList.Length; i++)
                {
                    RemoveContenNode(_tempNodeList[i]);
                }

                //重新加入已运行节点
                for (int i = 0; i < runningNode.Count; i++)
                {
                    AddContentNode(runningNode[i]);
                }
            }

            if (OnNodeLoaded != null)
                OnNodeLoaded.Invoke(this);
        }

        //private void SaveNode(BinaryWriter w,NodeModifier node)
        //{
        //    System.Type type = node.GetType();
        //    w.Write(type.FullName);

        //    long length = w.BaseStream.Length;
        //    node.Serialize(w);
        //}

    }
}