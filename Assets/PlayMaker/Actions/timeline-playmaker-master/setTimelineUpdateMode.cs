using UnityEngine;
using UnityEngine.Playables;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("Timeline")]
	[Tooltip("Set the current timelines time update mode (method). This controls how time is incremented when playing back. This action requires Unity 2017.1 or above.")]

	public class  setTimelineUpdateMode : FsmStateAction
	{
		[RequiredField]
		[CheckForComponent(typeof(PlayableDirector))]
		[Tooltip("The game object to hold the unity timeline components.")]
		public FsmOwnerDefault gameObject;
		
		[Tooltip("Set the time update mode.")]
		[ObjectType(typeof(DirectorUpdateMode))]
		public FsmEnum updateMode;
		
		[Tooltip("Check this box to preform this action every frame.")]
		public FsmBool everyFrame;

		private PlayableDirector timeline;

		public override void Reset()
		{
			gameObject = null;
			everyFrame = false;
		}

		public override void OnEnter()
		{
			var go = Fsm.GetOwnerDefaultTarget(gameObject);
			timeline = go.GetComponent<PlayableDirector>();

			if (!everyFrame.Value)
			{
				timelineAction();
				Finish();
			}

		}

		public override void OnUpdate()
		{
			if (everyFrame.Value)
			{
				timelineAction();
			}
		}

		void timelineAction()
		{
			var go = Fsm.GetOwnerDefaultTarget(gameObject);
			if (go == null)
			{
				return;
			}
			
			timeline.timeUpdateMode = (DirectorUpdateMode)updateMode.Value;
			
		}

	}
}