﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace CDTK {
	public class ConvexDecomposition : MonoBehaviour {
		
		public GameObject level;
		
		VertexGraph vg;

		public void TriangulateLevel() {

 			vg = Undo.AddComponent<VertexGraph>(gameObject);

			// //For the purposes of inserting a level manually through a list of preset Vector2 vars.
			// Vector2 [] vertices = new Vector2[] { <Insert Vector2s> };
			// vg.AddPolygon (vertices);
			// // Subsequent adding of obstacles is done through listing it as a separate list of vertices

				// Gets the polygon objects out of the level component, from GRTK. If using a different method of supplying the polygon, this needs to be changed.
			GRTK.Polygon [] polyArray = level.GetComponents<GRTK.Polygon>();
				// adds all the polygons from the GRTK to the vertex graph. Starts at 1 rather than 0 because the outermost polygon in the GRTK method of level building is not part of the level.
			for(int i = 1; i < polyArray.Length; i++)
				vg.AddPolygon (polyArray[i].GetRaw2());

				// First, need to draw the lines between vertices, granted they don't intersect any other lines (boundaries or drawn)
			foreach(Vertex start in vg.GetVertices())
				foreach (Vertex end in vg.GetVertices()) {
					Line possibleEdge = new Line(start, end);
					if (vg.IntersectsNothing(possibleEdge) && !end.Equals(start))
						vg.DrawEdge(possibleEdge);
				}

				// Now, to delete all lines which are either in obstacles or exterior to the level.
				// If a point on a drawn line which has an infinite line drawn off in some direction has an even number of intersections with
				// the boundaries of the polygon, it is either outside of it or in an obstacle. This completes triangulation.

			foreach(Line line in vg.GetDrawnEdges())
				if(NumIntersects(line,vg)%2 == 0) vg.EraseEdge(line);

				// Next, to calculate angles for and sort the edges stemming from each vertex, now that they're "finalized" for the purposes of triangulation.
			foreach(Vertex v in vg.GetVertices())
				v.SortEdges();			
			
			vg.createRegions();

		}
		public void DecomposeLevel () {

				// So in order to combine two regions and add the combined one, we need to take care of a few bookeeping things.
				// Remove boundary line.
				// Remove both original regions.
				// Add the new one.
				// This needs to be done for each drawn line in the vg.	
			foreach(Line l in vg.GetDrawnEdges()){
				Region r = new Region(l.cw, l.ccw, l);
				if(r.IsConvex()){
					vg.RemoveRegion(l.cw); vg.RemoveRegion(l.ccw); vg.EraseEdge(l);
					vg.AddRegion(r);
				}
			}	
		}

		// Counts the number of times an infinite line drawn between the point on the line given and some point
		// outside the polygon intersects the boundaries of the polygon.
		public int NumIntersects(Line line, VertexGraph vg){
			int count = 0;
			bool skip = false;
			int ind;

			// Creates the "infinite" line, from a point on the drawn line to somewhere outside the polygon
			Line infLine = new Line(new Vector2(((line.a.val.x + line.b.val.x)/2),((line.a.val.y + line.b.val.y)/2)), new Vector2(vg.maximumX() + 1,vg.maximumY() + 1));

			Line [] edges = vg.GetBoundEdges();
			foreach(Line bound in edges)
			{
				// If the infline intersects with the bound edge, increment counter.
				// However, we want to skip the count if:
				// This is the second of two edges which connect at a vertex the infline intersects
				// (going from inside the shape to outside or vice versa)
				// Both lines if its at a vertex where both lines are on the same side of the infline.
				//( staying either inside or outside of the shape)
				// So to generalize:
					// Theres no skip business at all if it's not intersecting a vertex. 
					// If it is intersecting a vertex, then check if the lines fall on the same side of the line.
						// If they are, skip both this and the next line.
						// If they're not, skip only this one and not the next.
				if(Array.IndexOf(edges, bound) + 1 == edges.Length - 1) ind = Array.IndexOf(edges, bound) + 1; else ind = 0;
				int otherPoint1; int otherPoint2;
				if(Vertex.Orientation(infLine.a, infLine.b, bound.a) == 0) otherPoint1 = Vertex.Orientation(infLine.a, infLine.b, bound.b); else otherPoint1 = Vertex.Orientation(infLine.a, infLine.b, bound.a);
				if(Vertex.Orientation(infLine.a, infLine.b, edges[ind].a) == 0) otherPoint2 = Vertex.Orientation(infLine.a, infLine.b, edges[ind].b); else otherPoint2 = Vertex.Orientation(infLine.a, infLine.b, edges[ind].a);
				
				// This part is tricky.
				// Check if the lines are on the same side or opposite sides of the infLine.
				// Opposite sides: dont skip this one, but do skip the next.
				// Same side: skip both this and the next.
				// Therefore, either way, we're skipping the next one. However, if they're on the same side, count++ right now.
						
				if(skip) continue;
				if(infLine.Intersect(bound))
					if(infLine.onInfLine(bound)){
						if(otherPoint1 != otherPoint2) count++;
						skip = true;
					}
					else count++;

			}
			return count;
		}

	}
}
