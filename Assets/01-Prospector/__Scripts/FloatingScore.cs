using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// an enum to track the possible states of a FloatingScore
public enum eFSState
{
    idle,
    pre,
    active,
    post
}

// FloatingScore can move itself on screen following a Bezier curve
public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public eFSState state = eFSState.idle;

    [SerializeField]
    protected int _score = 0;
    public string scoreString;

    // the score property sets both _score and scoreString
    public int score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            scoreString = _score.ToString("N0"); // adds commas to the num
            GetComponent<Text>().text = scoreString;
        }
    }

    public List<Vector2> bezierPts;
    public List<float> fontSizes;
    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut;

    // the GameObject that will receive the SendMessage when this is done moving
    public GameObject reportFinishTo = null;
    private RectTransform rectTrans;
    private Text txt;

    // Set up the FloatingScore and mvmt
    // not the use of parameter defaults for eTimeS & eTimeD
    public void Init(List<Vector2> ePts, float eTimeS = 0, float eTimeD = 1)
    {
        rectTrans = GetComponent<RectTransform>();
        rectTrans.anchoredPosition = Vector2.zero;
        txt = GetComponent<Text>();
        bezierPts = new List<Vector2>(ePts);

        if (ePts.Count == 1) // if theres only one point
        {
            // then just go there
            transform.position = ePts[0];
            return;
        }

        // if eTimeS is the default, just start at the current time
        if (eTimeS == 0) eTimeS = Time.time;
        timeStart = eTimeS;
        timeDuration = eTimeD;
        state = eFSState.pre; // set it to pre state, ready to start moving
    }

    public void FSCallback(FloatingScore fs)
    {
        // when this callback is called by SendMessage,
        // add the score from the calling FloatingScore
        score += fs.score;
    }

    // Update is called once per frame
    void Update()
    {
        // if this is not moving, just return
        if (state == eFSState.idle) return;

        // get u from the current time and duration
        // u ranges from 0 to 1 usually
        float u = (Time.time - timeStart) / timeDuration;
        // use easing class from Utils to curve the u value
        float uC = Easing.Ease(u, easingCurve);
        if (u < 0)
        {
            // we shouldn't move yet
            state = eFSState.pre;
            txt.enabled = false; // hide score initially
        } else
        {
            if (u >= 1)
            {
                // we're done moving
                uC = 1;
                state = eFSState.post;
                if (reportFinishTo != null) // if there's a callback GameObject
                {
                    // use SendMessage to call the FSCallback method
                    // with this as the parameter
                    reportFinishTo.SendMessage("FSCallback", this);

                    // now that message is sent
                    // destroy this gameObject
                    Destroy(gameObject);
                } else
                {
                    // if there's nothing to callback...
                    // then don't destroy this. Just let it stay still
                    state = eFSState.idle;
                }
            } else
            {
                // 0 <= u < 1, which means that this is active and moving
                state = eFSState.active;
                txt.enabled = true;
            }

            // use bezier curve to move this to right point
            Vector2 pos = Utils.Bezier(uC, bezierPts);
            // RectTransform anchors can be used to position UI objects
            // to total size of the screen
            rectTrans.anchorMin = rectTrans.anchorMax = pos;
            if (fontSizes != null && fontSizes.Count > 0)
            {
                // if fontsizes has values in it
                // then adjust the fontSize of this GUIText
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
                GetComponent<Text>().fontSize = size;
            }
        }
    }
}
