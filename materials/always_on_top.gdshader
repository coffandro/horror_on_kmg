shader_type spatial;
render_mode unshaded, depth_test_disabled;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    vec3 color = texture(screen_texture, SCREEN_UV).rgb;
    ALBEDO = color;
}