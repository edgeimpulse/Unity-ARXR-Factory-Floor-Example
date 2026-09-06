/*
 * ei_fomo.cpp — thin C ABI wrapper around the Edge Impulse inferencing SDK,
 * exposing FOMO object detection to Unity via P/Invoke.
 *
 * The model (Edge Impulse project 751472, "Plastic Bottle Caps") is a FOMO
 * detector with a 96x96 RGB input and two classes: cap_correct, cap_incorrect.
 */

#include <cstring>
#include <cstdint>
#include "edge-impulse-sdk/classifier/ei_run_classifier.h"

extern "C" {

// Must match the [StructLayout(Sequential)] struct on the C# side.
struct EiBoundingBox {
    char  label[32];
    float value;   // confidence 0..1
    int   x;       // top-left, in input-image pixels
    int   y;
    int   width;
    int   height;
};

}

// Pixels for the frame currently being classified (RGB, width*height*3 bytes).
static const uint8_t *g_pixels = nullptr;

// Edge Impulse image models pack each pixel into one float as 0xRRGGBB.
static int ei_fomo_get_data(size_t offset, size_t length, float *out_ptr) {
    for (size_t i = 0; i < length; i++) {
        const uint8_t *p = &g_pixels[(offset + i) * 3];
        out_ptr[i] = (float)((p[0] << 16) + (p[1] << 8) + p[2]);
    }
    return EIDSP_OK;
}

extern "C" int ei_fomo_input_width()  { return EI_CLASSIFIER_INPUT_WIDTH;  }
extern "C" int ei_fomo_input_height() { return EI_CLASSIFIER_INPUT_HEIGHT; }

/*
 * Run FOMO on an RGB frame. `rgb` must be width*height*3 bytes and match the
 * model input size. Fills up to `max_boxes` detections and returns the count,
 * or a negative value on error.
 */
extern "C" int ei_fomo_classify(const uint8_t *rgb, int width, int height,
                                EiBoundingBox *out, int max_boxes) {
    if (rgb == nullptr || out == nullptr) return -1;
    if (width != EI_CLASSIFIER_INPUT_WIDTH || height != EI_CLASSIFIER_INPUT_HEIGHT) return -2;

    g_pixels = rgb;

    signal_t signal;
    signal.total_length = EI_CLASSIFIER_INPUT_WIDTH * EI_CLASSIFIER_INPUT_HEIGHT;
    signal.get_data = &ei_fomo_get_data;

    ei_impulse_result_t result;
    memset(&result, 0, sizeof(result));

    EI_IMPULSE_ERROR err = run_classifier(&signal, &result, false);
    if (err != EI_IMPULSE_OK) return -100 - (int)err;

    int n = 0;
    for (size_t i = 0; i < result.bounding_boxes_count && n < max_boxes; i++) {
        ei_impulse_result_bounding_box_t bb = result.bounding_boxes[i];
        if (bb.value == 0.0f) continue; // FOMO emits empty boxes for background
        std::strncpy(out[n].label, bb.label, sizeof(out[n].label) - 1);
        out[n].label[sizeof(out[n].label) - 1] = '\0';
        out[n].value  = bb.value;
        out[n].x      = (int)bb.x;
        out[n].y      = (int)bb.y;
        out[n].width  = (int)bb.width;
        out[n].height = (int)bb.height;
        n++;
    }
    return n;
}
