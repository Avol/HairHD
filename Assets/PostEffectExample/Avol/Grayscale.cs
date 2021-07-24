using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Grayscale : ScriptableRendererFeature
{
    [System.Serializable]
    public class MyFeatureSettings
    {
        public bool IsEnabled = true;
    }

    public MyFeatureSettings settings = new MyFeatureSettings();


    CustomRenderPass myRenderPass;

    class CustomRenderPass : ScriptableRenderPass
    {
        RenderTargetIdentifier cameraColorTargetIdent;
        Material materialToBlit;
        RenderTargetHandle tempTexture;

        public CustomRenderPass()
        {
            this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            this.materialToBlit = Resources.Load<Material>("Grayscale");
        }

        // called each frame before Execute, use it to set up things the pass will need
       /* public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // create a temporary render texture that matches the camera
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
        } */


        public void Setup(RenderTargetIdentifier rt)
        {
            this.cameraColorTargetIdent= rt;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // fetch a command buffer to use
            CommandBuffer cmd = CommandBufferPool.Get("Custom render pass tag");
            cmd.Clear();

            // the actual content of our custom render pass!
            // we apply our material while blitting to a temporary texture
            cmd.Blit(cameraColorTargetIdent, cameraColorTargetIdent, materialToBlit, 0);

            // don't forget to tell ScriptableRenderContext to actually execute the commands
            context.ExecuteCommandBuffer(cmd);

            // tidy up after ourselves
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }


    /// <inheritdoc/>
    public override void Create()
    {
        myRenderPass = new CustomRenderPass();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        myRenderPass.Setup(cameraColorTargetIdent);
        renderer.EnqueuePass(myRenderPass); 
    }
}


