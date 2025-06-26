using Core.Player;
using UnityEngine;

namespace Adapters.Character
{
    public class PlayerView : MonoBehaviour
    {
        protected IPlayer _player;
        public Material mat;

        public virtual void Setup(IPlayer player)
        {
            _player = player;
            enabled = true;
        }

        protected virtual void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _player.Position, 0.8f);
            //transform.rotation = Quaternion.Slerp(transform.rotation, _player.Rotation, 0.8f);
            transform.rotation = Quaternion.Slerp(
            transform.rotation,
                Quaternion.Euler(0, _player.Rotation, 0),
                0.8f
            );
        }

        public void SetMaterialToTransparent()
        {
            if (mat == null) return;

            mat.SetFloat("_Mode", 3); // 3 = Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Color color = mat.color;
            color.a = 72f / 255f;
            mat.color = color;
        }

        public void SetMaterialToOpaque()
        {
            if (mat == null) return;

            mat.SetFloat("_Mode", 0); // 0 = Opaque
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1; // default render queue for opaque
            Color color = mat.color;
            color.a = 255f / 255f;
            mat.color = color;
        }
    }
}
