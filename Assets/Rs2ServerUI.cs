using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Eidetic.Rs2
{
    public partial class Rs2Server : MonoBehaviour
    {
        void InitialiseUI()
        {
            for (int i = 0; i < Drivers.Count(); i++)
            {
                var cam = Cameras[i];
                var previewImage = GameObject.Find($"ColorTexture{i}")
                    .GetComponent<RawImage>();

                var enableButton = GameObject.Find($"EnableButton{i}")
                    .GetComponent<Button>();
                enableButton.onClick.RemoveAllListeners();
                enableButton.onClick.AddListener(() =>
                {
                    cam.Active = !cam.Active;
                    var enableLabel = enableButton.gameObject
                        .GetComponentsInChildren<Text>().First();
                    enableLabel.text = cam.Active ? "Disable" : "Enable";
                    if (!cam.Active) previewImage.texture = null;
                });
                var pauseButton = GameObject.Find($"PauseButton{i}")
                    .GetComponent<Button>();
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() =>
                {
                    cam.Paused = !cam.Paused;
                    var pauseLabel = pauseButton.gameObject
                        .GetComponentsInChildren<Text>().First();
                    pauseLabel.text = cam.Paused ? "Resume" : "Pause";
                });
                var brightnessSlider = GameObject.Find($"BrightnessSlider{i}")
                    .GetComponent<Slider>();
                brightnessSlider.onValueChanged.RemoveAllListeners();
                brightnessSlider.value = cam.Brightness;
                brightnessSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Brightness = newVal );

                var saturationSlider = GameObject.Find($"SaturationSlider{i}")
                    .GetComponent<Slider>();
                saturationSlider.onValueChanged.RemoveAllListeners();
                saturationSlider.value = cam.Saturation;
                saturationSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Saturation = newVal );

                var exposureSlider = GameObject.Find($"ExposureSlider{i}")
                    .GetComponent<Slider>();
                exposureSlider.onValueChanged.RemoveAllListeners();
                exposureSlider.value = cam.Exposure;
                exposureSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Exposure = newVal );

                var gainSlider = GameObject.Find($"GainSlider{i}")
                    .GetComponent<Slider>();
                gainSlider.onValueChanged.RemoveAllListeners();
                gainSlider.value = cam.Gain;
                gainSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Gain = newVal );

                var contrastSlider = GameObject.Find($"ContrastSlider{i}")
                    .GetComponent<Slider>();
                contrastSlider.onValueChanged.RemoveAllListeners();
                contrastSlider.value = cam.Contrast;
                contrastSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Contrast = newVal );

                var pointThresholdSliderXMin = GameObject.Find($"PointThresholdXMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderXMax = GameObject.Find($"PointThresholdXMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderXMin.onValueChanged.RemoveAllListeners();
                pointThresholdSliderXMin.value = cam.PointThresholdMin.x;
                pointThresholdSliderXMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = newVal;
                    var y = cam.PointThresholdMin.y;
                    var z = cam.PointThresholdMin.z;
                    cam.PointThresholdMin = new Vector3(x, y, z);
                });
                pointThresholdSliderXMax.onValueChanged.RemoveAllListeners();
                pointThresholdSliderXMax.value = cam.PointThresholdMax.x;
                pointThresholdSliderXMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = newVal;
                    var y = cam.PointThresholdMax.y;
                    var z = cam.PointThresholdMax.z;
                    cam.PointThresholdMax = new Vector3(x, y, z);
                });
                var pointThresholdSliderYMin = GameObject.Find($"PointThresholdYMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderYMax = GameObject.Find($"PointThresholdYMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderYMin.onValueChanged.RemoveAllListeners();
                pointThresholdSliderYMin.value = cam.PointThresholdMin.y;
                pointThresholdSliderYMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThresholdMin.x;
                    var y = newVal;
                    var z = cam.PointThresholdMin.z;
                    cam.PointThresholdMin = new Vector3(x, y, z);
                });
                pointThresholdSliderYMax.onValueChanged.RemoveAllListeners();
                pointThresholdSliderYMax.value = cam.PointThresholdMax.y;
                pointThresholdSliderYMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThresholdMax.x;
                    var y = newVal;
                    var z = cam.PointThresholdMax.z;
                    cam.PointThresholdMax = new Vector3(x, y, z);
                });
                var pointThresholdSliderZMin = GameObject.Find($"PointThresholdZMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderZMax = GameObject.Find($"PointThresholdZMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderZMin.onValueChanged.RemoveAllListeners();
                pointThresholdSliderZMin.value = cam.PointThresholdMin.z;
                pointThresholdSliderZMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThresholdMin.x;
                    var y = cam.PointThresholdMin.y;
                    var z = newVal;
                    cam.PointThresholdMin = new Vector3(x, y, z);
                });
                pointThresholdSliderZMax.onValueChanged.RemoveAllListeners();
                pointThresholdSliderZMax.value = cam.PointThresholdMax.z;
                pointThresholdSliderZMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThresholdMax.x;
                    var y = cam.PointThresholdMax.y;
                    var z = newVal;
                    cam.PointThresholdMax = new Vector3(x, y, z);
                });

                var postTranslateSliderXCoarse = GameObject.Find($"PostTranslateXCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderXFine = GameObject.Find($"PostTranslateXFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderXCoarse.onValueChanged.RemoveAllListeners();
                postTranslateSliderXCoarse.value = cam.PostTranslation.x;
                postTranslateSliderXCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderXFine.value;
                    var x = coarseVal + fineVal;
                    var y = cam.PostTranslation.y;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderXFine.onValueChanged.RemoveAllListeners();
                postTranslateSliderXFine.value = 0;
                postTranslateSliderXFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderXCoarse.value;
                    var x = coarseVal + fineVal;
                    var y = cam.PostTranslation.y;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                var postTranslateSliderYCoarse = GameObject.Find($"PostTranslateYCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderYFine = GameObject.Find($"PostTranslateYFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderYCoarse.onValueChanged.RemoveAllListeners();
                postTranslateSliderYCoarse.value = cam.PostTranslation.y;
                postTranslateSliderYCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderYFine.value;
                    var x = cam.PostTranslation.x;
                    var y = coarseVal + fineVal;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderYFine.onValueChanged.RemoveAllListeners();
                postTranslateSliderYFine.value = 0;
                postTranslateSliderYFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderYCoarse.value;
                    var x = cam.PostTranslation.x;
                    var y = coarseVal + fineVal;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                var postTranslateSliderZCoarse = GameObject.Find($"PostTranslateZCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderZFine = GameObject.Find($"PostTranslateZFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderZCoarse.onValueChanged.RemoveAllListeners();
                postTranslateSliderZCoarse.value = cam.PostTranslation.z;
                postTranslateSliderZCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderZFine.value;
                    var x = cam.PostTranslation.x;
                    var y = cam.PostTranslation.y;
                    var z = coarseVal + fineVal;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderZFine.onValueChanged.RemoveAllListeners();
                postTranslateSliderZFine.value = 0;
                postTranslateSliderZFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderZCoarse.value;
                    var x = cam.PostTranslation.x;
                    var y = cam.PostTranslation.y;
                    var z = coarseVal + fineVal;
                    cam.PostTranslation = new Vector3(x, y, z);
                });

                var rotateSliderXCoarse = GameObject.Find($"RotateXCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderXFine = GameObject.Find($"RotateXFine{i}")
                    .GetComponent<Slider>();
                rotateSliderXCoarse.onValueChanged.RemoveAllListeners();
                rotateSliderXCoarse.value = cam.Rotation.x / 36f;
                rotateSliderXCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderXFine.value;
                    var x = (coarseVal * 36) + (fineVal * 1);
                    var y = cam.Rotation.y;
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderXFine.onValueChanged.RemoveAllListeners();
                rotateSliderXFine.value = 0;
                rotateSliderXFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderXCoarse.value;
                    var x = (coarseVal * 36) + (fineVal * 1);
                    var y = cam.Rotation.y;
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                var rotateSliderYCoarse = GameObject.Find($"RotateYCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderYFine = GameObject.Find($"RotateYFine{i}")
                    .GetComponent<Slider>();
                rotateSliderYCoarse.onValueChanged.RemoveAllListeners();
                rotateSliderYCoarse.value = cam.Rotation.y / 36f;
                rotateSliderYCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderYFine.value;
                    var x = cam.Rotation.x;
                    var y = (coarseVal * 36) + (fineVal * 1);
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderYFine.onValueChanged.RemoveAllListeners();
                rotateSliderYFine.value = 0;
                rotateSliderYFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderYCoarse.value;
                    var x = cam.Rotation.x;
                    var y = (coarseVal * 36) + (fineVal * 1);
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                var rotateSliderZCoarse = GameObject.Find($"RotateZCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderZFine = GameObject.Find($"RotateZFine{i}")
                    .GetComponent<Slider>();
                rotateSliderZCoarse.onValueChanged.RemoveAllListeners();
                rotateSliderZCoarse.value = cam.Rotation.z / 36f;
                rotateSliderZCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderZFine.value;
                    var x = cam.Rotation.x;
                    var y = cam.Rotation.y;
                    var z = (coarseVal * 36) + (fineVal * 1);
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderZFine.onValueChanged.RemoveAllListeners();
                rotateSliderZFine.value = 0;
                rotateSliderZFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderZCoarse.value;
                    var x = cam.Rotation.x;
                    var y = cam.Rotation.y;
                    var z = (coarseVal * 36) + (fineVal * 1);
                    cam.Rotation = new Vector3(x, y, z);
                });
            }

            var calibrationButton = GameObject.Find("CalibrateButton")
                .GetComponent<Button>();
            calibrationButton.onClick.RemoveAllListeners();
            calibrationButton.onClick.AddListener(() => RunCalibration());

            var markerSizeDropdown = GameObject.Find("MarkerSizeDropdown")
                .GetComponent<Dropdown>();
            markerSizeDropdown.onValueChanged.RemoveAllListeners();
            markerSizeDropdown.onValueChanged.AddListener((index) => {
                var selection = markerSizeDropdown.options[index].text;
                if (selection.Contains("A3"))
                    ArucoSize = MarkerSize.A3;
                else if (selection.Contains("A4"))
                    ArucoSize = MarkerSize.A4;
            });

            var pauseAllButton = GameObject.Find("PauseAllButton")
                .GetComponent<Button>();
            pauseAllButton.onClick.RemoveAllListeners();
            pauseAllButton.onClick.AddListener(() => {
                for (int i = 0; i < CameraCount; i++)
                {
                    var pauseButton = GameObject.Find($"PauseButton{i}")
                        .GetComponent<Button>();
                    pauseButton.onClick.Invoke();
                }
                var pauseAllLabel = pauseAllButton.GetComponentsInChildren<Text>().First();
                pauseAllLabel.text = Cameras.First().Paused ? "Resume All Cameras" : "Pause All Cameras";
            });

            var saveButton = GameObject.Find("SaveButton")
                .GetComponent<Button>();
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => Serialize());
            var loadButton = GameObject.Find("LoadButton")
                .GetComponent<Button>();
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() => Deserialize(CurrentConfigName));

            var configName = GameObject.Find("ConfigName")
                .GetComponent<InputField>();
            configName.onEndEdit.RemoveAllListeners();
            configName.onEndEdit.AddListener((name) => CurrentConfigName = name);
            configName.text = CurrentConfigName;

            var spoutOutputToggle = GameObject.Find("SpoutOutputToggle")
                .GetComponent<Toggle>();
            spoutOutputToggle.onValueChanged.RemoveAllListeners();
            spoutOutputToggle.onValueChanged.AddListener((toggleVal) => SendOverSpout = toggleVal);

            var customHookToggle = GameObject.Find("CustomHookToggle")
                .GetComponent<Toggle>();
            customHookToggle.onValueChanged.RemoveAllListeners();
            customHookToggle.onValueChanged.AddListener((toggleVal) => RunCustomHooks = toggleVal);

            var networkHostnameInput = GameObject.Find("HostnameInput")
                .GetComponent<InputField>();
            networkHostnameInput.onEndEdit.RemoveAllListeners();
            networkHostnameInput.onEndEdit.AddListener((hostname) => Hostname = hostname);

            var networkConnectButton = GameObject.Find("ConnectButton")
                .GetComponent<Button>();
            networkConnectButton.onClick.RemoveAllListeners();
            networkConnectButton.onClick.AddListener(() => NetworkConnect());

            var networkDisconnectButton = GameObject.Find("DisconnectButton")
                .GetComponent<Button>();
            networkDisconnectButton.onClick.RemoveAllListeners();
            networkDisconnectButton.onClick.AddListener(() => NetworkDisconnect());

            FpsCounter = GameObject.Find("FPS").GetComponent<Text>();
        }

        Text FpsCounter;
        int FrameCount = 0;
        float Delta = 0;
        float CounterUpdateRate = 4;

        void LateUpdate()
        {
            FrameCount++;
            Delta += Time.deltaTime;
            if (Delta > 1.0f / CounterUpdateRate)
            {
                FpsCounter.text = $"FPS: {Mathf.RoundToInt(FrameCount / Delta)}";
                FrameCount = 0;
                Delta -= 1.0f / CounterUpdateRate;
            }

        }
    }
}
