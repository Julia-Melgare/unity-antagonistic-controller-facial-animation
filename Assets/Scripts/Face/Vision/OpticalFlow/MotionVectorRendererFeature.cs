using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MotionVectorRendererFeature : ScriptableRendererFeature
{
	class MotionVectorPass : ScriptableRenderPass
	{
		private Material _material;
		private Mesh _mesh;

		public MotionVectorPass(Material material, Mesh mesh)
		{
			_material = material;
			_mesh = mesh;
		}

		public override void Execute(ScriptableRenderContext context,
			ref RenderingData renderingData)
		{
			
			CommandBuffer cmd = CommandBufferPool.Get(name: "MotionVectorPass");

			/// Get the Camera data from the renderingData argument.
			Camera camera = renderingData.cameraData.camera;
			// Set the projection matrix so that Unity draws the quad in screen space
			cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
			// Add the scale variable, use the Camera aspect ratio for the y coordinate
			Vector3 scale = new Vector3(1, camera.aspect, 1);

			cmd.DrawMesh(_mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale*2f), _material, 0, 0);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
			
		}
	}

	private MotionVectorPass _motionVectorPass;
	public Material material;
	public Mesh mesh;

	public override void Create()
	{
		_motionVectorPass = new MotionVectorPass(material, mesh);
		_motionVectorPass.renderPassEvent = RenderPassEvent.AfterRendering;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (material != null && mesh != null)
		{
			_motionVectorPass.ConfigureInput(ScriptableRenderPassInput.Motion);
			renderer.EnqueuePass(_motionVectorPass);
		}
	}
}