using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controla las animaciones del personaje detectando automáticamente
/// qué clips están disponibles en su AnimatorController en runtime.
/// Se adjunta al GameObject del personaje tras instanciarlo (ver PlayerSpawner).
/// </summary>
public class PersonajeAnimador : MonoBehaviour
{
    [System.Serializable]
    struct ClipsPersonaje
    {
        public string idle;
        public string run;
        public string victory;
    }

    // Clip names sin extensión, ordenados por prioridad de detección.
    // La detección usa el clip de idle como fingerprint del personaje.
    private static readonly ClipsPersonaje[] _tablaPersonajes = {
        new ClipsPersonaje {
            idle    = "ghost_idle",
            run     = "ghost_run",
            victory = "ghost_surprised"
        },
        new ClipsPersonaje {
            idle    = "Idle_Battle",
            run     = "RunForwardBattle",
            victory = "Idle_Battle"
        },
        new ClipsPersonaje {
            idle    = "Idle_Normal_SwordAndShield",
            run     = "MoveFWD_Normal_InPlace_SwordAndShield",
            victory = "Victory_Battle_SwordAndShield"
        },
    };

    private Animator        _animator;
    private ClipsPersonaje  _clips;
    private bool            _detectado;
    private Coroutine       _victoriaCoroutine;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>(includeInactive: true);
        if (_animator == null)
        {
            Debug.LogWarning($"[PersonajeAnimador] {name}: no se encontró Animator.");
            return;
        }

        _clips    = DetectarClips(_animator);
        _detectado = true;

        // Arrancar en idle
        PlayClip(_clips.idle, 0f);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void SetMoviendo(bool moviéndose)
    {
        if (!_detectado) return;
        PlayClip(moviéndose ? _clips.run : _clips.idle, 0.15f);
    }

    public void SetVictoria()
    {
        if (!_detectado) return;
        if (_victoriaCoroutine != null) StopCoroutine(_victoriaCoroutine);
        _victoriaCoroutine = StartCoroutine(VictoriaYVolver());
    }

    // ── Detección automática ──────────────────────────────────────────────────

    private static ClipsPersonaje DetectarClips(Animator animator)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController?.animationClips;

        if (clips != null && clips.Length > 0)
        {
            foreach (var tabla in _tablaPersonajes)
            {
                if (clips.Any(c => c.name == tabla.idle))
                {
                    Debug.Log($"[PersonajeAnimador] Personaje detectado por clip idle='{tabla.idle}'");
                    return tabla;
                }
            }
        }

        // Fallback: DogKnight (el más común, tiene idle=Idle_Battle)
        Debug.LogWarning("[PersonajeAnimador] No se detectó personaje por clips — usando fallback DogKnight.");
        return _tablaPersonajes[1];
    }

    // ── Reproducción ──────────────────────────────────────────────────────────

    private void PlayClip(string clipName, float transitionDuration)
    {
        if (_animator == null || string.IsNullOrEmpty(clipName)) return;

        if (transitionDuration > 0f)
        {
            // CrossFade necesita que el estado exista en el AnimatorController.
            // Si falla (estado desconocido) Unity lo ignora silenciosamente; el fallback
            // con Play() a continuación solo se ejecuta si el animator tiene un override
            // controller sin ese estado — en la práctica CrossFade es suficiente.
            _animator.CrossFade(clipName, transitionDuration);
        }
        else
        {
            _animator.Play(clipName);
        }
    }

    private IEnumerator VictoriaYVolver()
    {
        PlayClip(_clips.victory, 0.1f);
        yield return new WaitForSeconds(3f);
        PlayClip(_clips.idle, 0.2f);
        _victoriaCoroutine = null;
    }
}
