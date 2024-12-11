using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierExtrudeTool
{
	public struct OrientedPoint {

		public Vector3 position;
		public Quaternion rotation;

		public OrientedPoint( Vector3 position, Quaternion rotation )
		{
			this.position = position;
			this.rotation = rotation;
		}

		public OrientedPoint( Vector3 position, Vector3 forward)
		{
			this.position = position;
			this.rotation = Quaternion.LookRotation(forward);
		}

		public Vector3 LocalToWorldPos( Vector3 localSpacePos )
		{
			return position + rotation * localSpacePos;
		}

		public Vector3 LocalToWorldVec( Vector3 localSpacePos )
		{
			return rotation * localSpacePos;
		}

		public Vector3 WorldToLocal( Vector3 localSpacePos )
		{
			return Quaternion.Inverse( rotation ) * ( localSpacePos - position );
		}

		public Vector3 LocalToWorldDirection( Vector3 dir )
		{
			return rotation * dir;
		}
	}
}

