#version 300 es

//This shader would work on the android app Shader Editor
//Translated using FragRoom version v1.4.1 - https://github.com/Dumbelfo08/FragRoom

#ifdef GL_FRAGMENT_PRECISION_HIGH
precision highp float;
#else
precision mediump float;
#endif

out vec4 fragColor;

vec2 TRANSLATOR_RA_fragCoord;
uniform float time;
uniform vec2 touch;
vec2 iTRANSLATOR_RA_Mouse;
uniform vec2 resolution;

  

vec3 palette(in float t){ //RGB palette in function of time
	float r = sin(t) * 0.5 + 0.5; //map the sine function to [0,1]
	float g = sin(t + 2.0*3.14*0.333) * 0.5 + 0.5; //map the sine function to [0,1] and introduces 120 degree phase
	float b = sin(t + 2.0*3.14*0.666) * 0.5 + 0.5; //map the sine function to [0,1] and introduces 240 degree phase
	
	return vec3(r, g, b);
}

void mainMethod(){
	vec2 coords = TRANSLATOR_RA_fragCoord;
	coords.x *= resolution.x/resolution.y; //Fix the aspect ratio
	
	vec2 mouseCoords = iTRANSLATOR_RA_Mouse;
	mouseCoords.x *= resolution.x/resolution.y; //Fix the aspect ratio for the mouse position too
	
	float dist = length(coords - mouseCoords); //calcultae distance to center
	float factor = 1.0 - step(sin(0.6*time)*0.4+0.6, dist); //calculate the factor with reverse step function
	
	vec3 color = palette(time) * factor; //Multiply by factor so only circe is colored
    fragColor = vec4(color, 1.0); //output
}
void main(){
    iTRANSLATOR_RA_Mouse = (touch / resolution) * 2.0 - 1.0;
    TRANSLATOR_RA_fragCoord = (gl_FragCoord.xy / resolution) * 2.0 - 1.0;
    mainMethod();
}