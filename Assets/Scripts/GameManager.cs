using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region MONOBEHAVIOUR METHODS

    public static GameManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text _scoreText, _endScoreText,_highScoreText, recordText;

    private int score;

    [SerializeField]
    private Animator _scoreAnimator;

    [SerializeField]
    private AnimationClip _scoreClip;
 
    [SerializeField]
    private GameObject _endPanel;

    [SerializeField]
    private Image _soundImage;

    [SerializeField]
    private Sprite _activeSoundSprite, _inactiveSoundSprite;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        ColorChanged += OnColorChanged; 
    }

    private void OnDisable()
    {
        ColorChanged -= OnColorChanged;
    }

    private void Start()
    {
        AudioManager.Instance.AddButtonSound();

        StartCoroutine(IStartGame());
        recordText.text = PlayerPrefs.GetInt(Constants.DATA.HIGH_SCORE).ToString();
    }

    #endregion

    #region UI

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(Constants.DATA.MAIN_MENU_SCENE);
    }

    public void ReloadGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToggleSound()
    {
        bool sound = (PlayerPrefs.HasKey(Constants.DATA.SETTINGS_SOUND) ? PlayerPrefs.GetInt(Constants.DATA.SETTINGS_SOUND)
            : 1) == 1;
        sound = !sound;
        PlayerPrefs.SetInt(Constants.DATA.SETTINGS_SOUND, sound ? 1 : 0);
        _soundImage.sprite = sound ? _activeSoundSprite : _inactiveSoundSprite;
        AudioManager.Instance.ToggleSound();
    }

    public void UpdateScore()
    {
        score++;
        _scoreText.text = score.ToString();
        _scoreAnimator.Play(_scoreClip.name, -1, 0f);

        SetUpScore();

        if(score % 2 == 0)
        {
            CurrentColorId =  (CurrentColorId + 1) % _colors.Count;
        }
    }

    #endregion

    #region GAME_START_END

    public void EndGame()
    {
        StartCoroutine(GameOver());
    }

    [SerializeField] private Animator _highScoreAnimator;
    [SerializeField] private AnimationClip _highScoreClip;

    private IEnumerator GameOver()
    {
        hasGameEnded = true;
        _scoreText.gameObject.SetActive(false);
        GameEnded?.Invoke();

        yield return new WaitForSeconds(1f);
        yield return MoveCamera (new Vector3(_cameraStartPos.x,-_cameraStartPos.y,_cameraStartPos.z));

        _endPanel.SetActive(true);
        _endPanel.GetComponent<Image>().color = CurrentColor;
        _endScoreText.text = score.ToString();

        bool sound = (PlayerPrefs.HasKey(Constants.DATA.SETTINGS_SOUND) ?
          PlayerPrefs.GetInt(Constants.DATA.SETTINGS_SOUND) : 1) == 1;
        _soundImage.sprite = sound ? _activeSoundSprite : _inactiveSoundSprite;

        int highScore = PlayerPrefs.HasKey(Constants.DATA.HIGH_SCORE) ? PlayerPrefs.GetInt(Constants.DATA.HIGH_SCORE) : 0;
        if (score > highScore)
        {
            _highScoreText.text = "NUEVO RECORD";

            //Play HighScore Animation
            _highScoreAnimator.Play(_highScoreClip.name,-1,0f);

            highScore = score;
            PlayerPrefs.SetInt(Constants.DATA.HIGH_SCORE, highScore);
        }
        else
        {
            _highScoreText.text = "RECORD " + highScore.ToString();
            recordText.text = PlayerPrefs.GetInt(Constants.DATA.HIGH_SCORE, 0).ToString();
        }
    }


    [SerializeField]
    private Vector3 _cameraStartPos, _cameraEndPos;
    [SerializeField]
    private float _timeToMoveCamera;

    private bool hasGameEnded;
    public UnityAction GameStarted, GameEnded;

    private IEnumerator IStartGame()
    {
        hasGameEnded = false;
        Camera.main.transform.position = _cameraStartPos;
        _scoreText.gameObject.SetActive(false);
        yield return MoveCamera(_cameraEndPos);

        //Score
        _scoreText.gameObject.SetActive(true);
        score = 0;
        _scoreText.text = score.ToString();
        _scoreAnimator.Play(_scoreClip.name, -1, 0f);

        //Set the Color for Score
        CurrentScoreId = 0;

        //Set the Color
        _currentColorId = 0;

        StartCoroutine(SpawnObstacles());
        GameStarted?.Invoke();
    }

    private IEnumerator MoveCamera(Vector3 cameraPos)
    {
        Transform cameraTransform = Camera.main.transform;
        float timeElapsed = 0f;
        Vector3 startPos = cameraTransform.position;
        Vector3 offset = cameraPos - startPos;
        float speed = 1 / _timeToMoveCamera;
        while(timeElapsed < 1f)
        {
            timeElapsed += speed * Time.deltaTime;
            cameraTransform.position = startPos + timeElapsed * offset;
            yield return null;
        }
        cameraTransform.position = cameraPos;
    }
    #endregion

    #region OBSTACLE_SPAWNING
    [SerializeField]
    private GameObject _obstaclePrefab;

    [SerializeField]
    private float _obstacleSpawnTime;

    private IEnumerator SpawnObstacles()
    {
        var timeInterval = new WaitForSeconds(_obstacleSpawnTime);
        while(!hasGameEnded)
        {            
            Instantiate(_obstaclePrefab);
            yield return timeInterval;
        }
    }
    #endregion

    #region COLOR_CHANGE
    [SerializeField]
    private List<Color> _colors;

    [HideInInspector]
    public Color CurrentColor => _colors[CurrentColorId];

    [HideInInspector]
    public UnityAction<Color> ColorChanged;

    private int _currentColorId;

    private int CurrentColorId
    {
        get
        {
            return _currentColorId;
        }
        set
        {
            _currentColorId = value;
            ColorChanged?.Invoke(CurrentColor);
        }
    }

    [SerializeField] private Camera cam;
    private void OnColorChanged(Color col)
    {
        cam.backgroundColor = col;
    }

    #endregion

    #region SELECT_RANDOM_SCORE
    public UnityAction ScoreReset;
    public UnityAction<int> ScoreSet;

    [SerializeField]
    private int maxScoreId;

    private int _currentScoreId;
    public int CurrentScoreId
    {
        get
        {
            return _currentScoreId;
        }

        set
        {
            _currentScoreId = value;
            ScoreReset?.Invoke();
            ScoreSet?.Invoke(value);
        }
    }

    private void SetUpScore()
    {
        int temp = CurrentScoreId;

        while(temp == CurrentScoreId)
        {
            temp = UnityEngine.Random.Range(0,maxScoreId);
        }

        CurrentScoreId = temp;
    }
    #endregion
    public void BorraRecord()
    {
        PlayerPrefs.DeleteKey(Constants.DATA.HIGH_SCORE);
    }
}
