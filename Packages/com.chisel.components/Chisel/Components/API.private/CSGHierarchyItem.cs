﻿using Chisel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chisel.Components
{

	public sealed class CSGSceneHierarchy
	{
		public Scene                            Scene;
		public CSGModel                         DefaultModel;		// TODO: create this, but only when necessary.
		public readonly List<CSGHierarchyItem>  RootItems		= new List<CSGHierarchyItem>();
	}

	public sealed class CSGHierarchyItem
	{
		public static readonly Bounds EmptyBounds = new Bounds();

		public CSGHierarchyItem(CSGNode node) { Component = node; }

		public CSGHierarchyItem                 Parent;
		public readonly List<int>               SiblingIndices      = new List<int>();
		public readonly List<CSGHierarchyItem>  Children            = new List<CSGHierarchyItem>();

		public CSGSceneHierarchy    sceneHierarchy;
		public Scene                Scene;
		public Transform            Transform;
		public GameObject           GameObject;
		public readonly CSGNode     Component;
		
		public CSGModel Model
		{
			get
			{
				var iterator = this;
				do
				{
					var model = iterator.Component as CSGModel;
					if (!Equals(model, null))
						return model;
					iterator = iterator.Parent;
				} while (!Equals(iterator, null));
				return null;
			}
		}

		private Bounds				Bounds				= EmptyBounds;
		private Bounds              ChildBounds         = EmptyBounds;
		private bool				BoundsDirty			= true;
		private bool                ChildBoundsDirty	= true;

		public bool                 Registered			= false;
		public bool                 IsOpen				= true;

		public Matrix4x4            LocalToWorldMatrix  = Matrix4x4.identity;
		public Matrix4x4            WorldToLocalMatrix  = Matrix4x4.identity;

		// TODO: Move bounds handling code to separate class, keep this clean
		public void					UpdateBounds()
		{
			if (BoundsDirty)
			{
				if (Component)
				{
					if (!Transform)
						Transform = Component.transform;
					Bounds = Component.CalculateBounds();
					ChildBoundsDirty = true;
					BoundsDirty = false;
				}
			}
			if (ChildBoundsDirty)
			{
				ChildBounds = Bounds;
				// TODO: make this non-iterative
				for (int i = 0; i < Children.Count; i++)
					Children[i].EncapsulateBounds(ref ChildBounds);
				ChildBoundsDirty = false;
			}
		}

		public void		EncapsulateBounds(ref Bounds outBounds)
		{
			UpdateBounds();
			if (ChildBounds.size.sqrMagnitude != 0)
			{
				float magnitude = ChildBounds.size.sqrMagnitude;
				if (float.IsInfinity(magnitude) ||
					float.IsNaN(magnitude))
				{
					var transformation = LocalToWorldMatrix;
					var center = transformation.GetColumn(3);
					ChildBounds = new Bounds(center, Vector3.zero);
				}
				if (outBounds.size.sqrMagnitude == 0) outBounds = ChildBounds;
				else								  outBounds.Encapsulate(ChildBounds);
			}
		}

		public void		SetChildBoundsDirty()
		{
			if (ChildBoundsDirty)
				return;

			ChildBoundsDirty = true;
			if (Parent != null)
				Parent.SetChildBoundsDirty();
		}

		public void		SetBoundsDirty()
		{
			if (BoundsDirty)
				return;

			BoundsDirty = true;
			if (Parent != null)
				Parent.SetChildBoundsDirty();
		}
	}

}