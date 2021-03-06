using UnityEngine;
using UnityEngine.Playables;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("Timeline")]
	[Tooltip("Get the current timeline's inital time. This is the time at which the timeline should start. This action requires Unity 2017.1 or above.")]

	public class  getTimelineIntialTime : FsmStateAction
	{
		[RequiredField]
		[CheckForComponent(typeof(PlayableDirector))]
		[Tooltip("The game object to hold the unity timeline components.")]
		public FsmOwnerDefault gameObject;
		
		[Tooltip("Returns the current timeline intial time.")]
		[UIHint(UIHint.Variable)]
		public FsmFloat time;
			
		[Tooltip("Check this box to preform this action every frame.")]
		public FsmBool everyFrame;

		private PlayableDirector timeline;

		public override void Reset()
		{

			gameObject = null;
			everyFrame = false;
			time = null;
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
			
			time.Value = (float)timeline.initialTime;	
			
		}

	}
}