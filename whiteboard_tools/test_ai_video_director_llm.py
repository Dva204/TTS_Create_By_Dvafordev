"""Offline tests: uses a fake vision provider, never calls an external AI."""
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from PIL import Image
from ai_video_director_llm import (
    ModelClient, url_for, small_image, parse_answer, valid_events,
    suggest_with_model, remap_to_canvas
)

class FakeModel:
    def __init__(self, answer):
        self.answer=answer
        self.calls=[]
    def ask(self,text,image):
        self.calls.append((text,image))
        return self.answer


class Level2Tests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.root=Path(self.tmp.name)
        Image.new("RGB",(640,360),"white").save(self.root/"demo.png")
        self.cues=[
            (0,3,"In 1903, the first aircraft changed flight."),
            (3,6,"Now look closely at the aircraft wing."),
        ]
        self.project={"schemaVersion":1,
                      "scenes":[{"index":1,"cueFrom":1,"cueTo":2,
                                 "startMs":0,"endMs":6000,
                                 "styleId":"image-story","imagePath":"demo.png",
                                 "narration":"In 1903, the first aircraft changed flight. Now look closely at the aircraft wing.",
                                 "directionEvents":[]}]}
        self.response={"events":[
            {"kind":"text","cueIndex":1,"text":"1903","reason":"important year"},
            {"kind":"zoom","cueIndex":2,"target":"aircraft wing","zoom":1.10,
             "focusX":.75,"focusY":.32,"reason":"reveal the wing"}
        ]}
    def tearDown(self):
        self.tmp.cleanup()

    def test_portrait_source_focus_remapped_to_letterboxed_canvas(self):
        portrait = self.root / "portrait.png"
        Image.new("RGB", (300, 600), "white").save(portrait)
        x, y = remap_to_canvas(.75, .25, portrait)
        self.assertGreater(x, .5)
        self.assertLess(x, .65)
        self.assertAlmostEqual(y, .25)
    def test_vision_image_attachment_and_focus(self):
        model=FakeModel(self.response)
        result,count=suggest_with_model(self.project,self.cues,self.root,model,send_images=True)
        self.assertEqual(count,2)
        self.assertEqual(result["scenes"][0]["directionEvents"][0]["text"],"1903")
        self.assertAlmostEqual(result["scenes"][0]["directionEvents"][1]["focusX"],.75)
        self.assertEqual(result["scenes"][0]["directionEvents"][1]["source"],"ai-level2")
        self.assertTrue(model.calls[0][1])
        self.assertIn("previous",model.calls[0][0])
        self.assertEqual(self.project["scenes"][0]["directionEvents"],[])

    def test_no_image_no_spurious_zoom(self):
        model=FakeModel(self.response)
        result,count=suggest_with_model(self.project,self.cues,self.root,model,send_images=False)
        self.assertEqual(count,1)
        self.assertIsNone(model.calls[0][1])

    def test_invented_caption_rejected(self):
        self.response["events"][0]["text"]="1984 was the invention year"
        result,count=suggest_with_model(self.project,self.cues,self.root,
                                        FakeModel(self.response),send_images=False)
        self.assertEqual(count,0)

    def test_invalid_zoom_coordinates_rejected(self):
        self.response["events"][1]["focusX"]=9
        result,count=suggest_with_model(self.project,self.cues,self.root,
                                        FakeModel(self.response),send_images=True)
        self.assertEqual(count,1)

    def test_existing_manual_effects_kept(self):
        self.project["scenes"][0]["directionEvents"]=[{
            "kind":"text","text":"Manual","startMs":0,"durationMs":1000}]
        model=FakeModel(self.response)
        result,count=suggest_with_model(self.project,self.cues,self.root,model,send_images=True)
        self.assertEqual(count,0)
        self.assertEqual(len(model.calls),0)
        self.assertEqual(result["scenes"][0]["directionEvents"][0]["text"],"Manual")

    def test_fenced_json_parsed(self):
        tick=chr(96)*3
        self.assertEqual(parse_answer(tick+"json\n"+json.dumps(self.response)+"\n"+tick),self.response)

    def test_reject_non_json(self):
        with self.assertRaises(ValueError):
            parse_answer("I can't do that.")

    def test_remote_endpoint_https_required(self):
        with self.assertRaisesRegex(ValueError,"HTTPS"):
            url_for("openai","http://example.com/v1")
        self.assertTrue(url_for("openai","https://api.example.com/v1").endswith("/chat/completions"))

    def test_ollama_must_be_local(self):
        with self.assertRaises(ValueError):
            url_for("ollama","http://example.com:11434")

    def test_no_external_key_required_for_ollama(self):
        client=ModelClient("ollama","test-vision","http://127.0.0.1:11434")
        self.assertIn("api/chat",client.url)

    def test_camera_data_is_derived_from_image_input(self):
        data=small_image(self.root/"demo.png",True)
        self.assertIsInstance(data,str)
        self.assertGreater(len(data),100)

    def test_openai_request_layout_without_real_network(self):
        fake={"choices":[{"message":{"content":json.dumps(self.response)}}]}
        with patch("ai_video_director_llm.send_json",return_value=fake) as transport:
            with patch.dict("os.environ",{"OPENAI_API_KEY":"unit-test-only"}):
                c=ModelClient("openai","vision-model","https://api.example.com/v1")
                result=c.ask("Please consider the text",small_image(self.root/"demo.png",True))
        self.assertEqual(result,self.response)
        arguments=transport.call_args.args
        self.assertTrue(arguments[1]["messages"][1]["content"][1]["image_url"]["url"].startswith("data:image/jpeg;base64,"))
        self.assertEqual(arguments[2]["Authorization"],"Bearer unit-test-only")

if __name__=="__main__":
    unittest.main()
