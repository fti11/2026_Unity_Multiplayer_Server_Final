using System;
using System.Collections.Generic;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameResultManager : MonoBehaviour
{
    private FirebaseDatabase database;
    private DatabaseReference dbReference;
    private UnityMainThreadDispatcher mainDispatcher;

    [Header("Firebase Config")]
    [SerializeField] private string fbDatabaseUrl = "https://final-ada3c-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI Display")]
    [SerializeField] private Text textCoinDisplay;
    [SerializeField] private Text textHighScoreDisplay;
    [SerializeField] private Text textLogMessage;
    [SerializeField] private InputField inputFinalScore;

    [Header("Game Reward")]
    [SerializeField] private int baseRewardCoin = 100;

    private string userSessionKey;
    private int userCurrentCoin;
    private int userBestScore;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(fbDatabaseUrl);
        dbReference = database.RootReference;
        mainDispatcher = UnityMainThreadDispatcher.Instance();

        userSessionKey = PlayerPrefs.GetString("UserKey");
        if (string.IsNullOrEmpty(userSessionKey))
        {
            textLogMessage.text = "인증 세션이 만료되었습니다. 다시 로그인하세요.";
            return;
        }

        FetchUserCloudData();
    }

    void FetchUserCloudData()
    {
        dbReference.Child("UserInfo").Child(userSessionKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                mainDispatcher.Enqueue(() => { textLogMessage.text = "서버 데이터 동기화 실패"; });
                return;
            }

            DataSnapshot dataSnapshot = task.Result;

            userCurrentCoin = int.Parse(dataSnapshot.Child("Coin").Value.ToString());

            if (dataSnapshot.HasChild("Score"))
            {
                userBestScore = int.Parse(dataSnapshot.Child("Score").Value.ToString());
            }
            else
            {
                userBestScore = 0;
            }

            mainDispatcher.Enqueue(() =>
            {
                UpdateInterface();
                textLogMessage.text = "최신 게임 데이터 로드 완료";
            });
        });
    }

    void UpdateInterface()
    {
        textCoinDisplay.text = "보유 코인: " + userCurrentCoin + " G";
        textHighScoreDisplay.text = "개인 최고 기록: " + userBestScore + " 점";
    }

    public void ProcessGameSettlement()
    {
        if (!int.TryParse(inputFinalScore.text, out int clientScore))
        {
            textLogMessage.text = "올바른 점수 형식이 아닙니다. (숫자만 입력)";
            return;
        }

        userCurrentCoin += baseRewardCoin;

        bool isNewRecordAchieved = false;
        if (clientScore > userBestScore)
        {
            userBestScore = clientScore;
            isNewRecordAchieved = true;
        }

        Dictionary<string, object> uploadPacket = new Dictionary<string, object>();
        uploadPacket["Coin"] = userCurrentCoin;
        uploadPacket["Score"] = userBestScore;

        dbReference.Child("UserInfo").Child(userSessionKey).UpdateChildrenAsync(uploadPacket).ContinueWith(task =>
        {
            mainDispatcher.Enqueue(() =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    UpdateInterface();
                    if (isNewRecordAchieved)
                    {
                        textLogMessage.text = $"신기록 경신! 보상 {baseRewardCoin}G 지급 완료!";
                    }
                    else
                    {
                        textLogMessage.text = $"게임 종료. 보상 {baseRewardCoin}G 지급 (기록 유지)";
                    }
                }
                else
                {
                    textLogMessage.text = "데이터베이스 반영 실패";
                }
            });
        });
    }

    public void OnClickReturnToLobby()
    {
        SceneManager.LoadScene("LoginScene");
    }
}
