//This file is not used for anything. Its just the source of the strings

//Modal
const modal = document.querySelector('.modal');
const modalOverlay = document.querySelector('.modal-overlay');
const modalContent = document.querySelector('.modal-content');
const okButton = document.querySelector('.btn-ok');

function closeModal(){
    modal.style.display = 'none';
    modalOverlay.style.display = 'none';
}

okButton.addEventListener('click', closeModal);

function showAlert(message){
    modalContent.innerHTML = message.replace(/\n/g, '<br>');
    modal.style.display = 'flex';
    modalOverlay.style.display = 'block';
}

//Auto close
const secondsToClose = @secondsToClose * 1000;

//Shader source
const fragmentShaderSource = `@shaderSource`;

//Textures
const textureSource = [
	`Ldata:image/png;base64,data`,
	null,
	null,
	null,
	null,
	null,
	null,
	null
];

//Texture functions
function createTextureFromBase64(base64Data, gl, filter){
    const texture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, texture);
    
    const chosenFilter = (filter === 'N') ? gl.NEAREST : gl.LINEAR;
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, chosenFilter);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, chosenFilter);

    return new Promise((resolve, reject) => {
        const image = new Image();
        
        image.onload = function(){
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
            gl.generateMipmap(gl.TEXTURE_2D);
            resolve(texture);
        };

        image.onerror = function(){
            reject(new Error('Error loading the texture image'));
        };

        image.src = base64Data;
    });
}

function numToUnit(num, gl){
	switch(num){
		default:
		case 0:
		return gl.TEXTURE0;
		case 1:
		return gl.TEXTURE1;
		case 2:
		return gl.TEXTURE2;
		case 3:
		return gl.TEXTURE3;
		case 4:
		return gl.TEXTURE4;
		case 5:
		return gl.TEXTURE5;
		case 6:
		return gl.TEXTURE6;
		case 7:
		return gl.TEXTURE7;
		case 8:
		return gl.TEXTURE8;
	}
}

async function loadTexture(program, gl, id){
	if(textureSource[id] != null){
		gl.activeTexture(numToUnit(id + 1, gl));
		const t = await createTextureFromBase64(textureSource[id].substring(1), gl, textureSource[id][0]);
		gl.bindTexture(gl.TEXTURE_2D, t);
		gl.uniform1i(gl.getUniformLocation(program, 'iTexture[' + id + ']'), id + 1);
	}
}

//Shader source
const vertexShaderSource = `#version 300 es\nin vec2 aPos;out vec2 fragCoord;void main(){gl_Position = vec4(aPos, 0.0, 1.0);fragCoord = gl_Position.xy;}`;

//Buffer shader source
const bufferFragmentSource = `#version 300 es\nprecision highp float;out vec4 fragColor;in vec2 fragCoord;uniform sampler2D buffer;void main(){fragColor = texture(buffer, fragCoord / 2.0 + 0.5);}`;

//Buffer
function createFramebuffer(gl, width, height){
    const framebuffer = gl.createFramebuffer();
    gl.bindFramebuffer(gl.FRAMEBUFFER, framebuffer);

    const texture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, texture);

    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);

    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, width, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);

    gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, texture, 0);

    if (gl.checkFramebufferStatus(gl.FRAMEBUFFER) !== gl.FRAMEBUFFER_COMPLETE){
        showAlert("Framebuffer is not complete.");
    }

    gl.bindFramebuffer(gl.FRAMEBUFFER, null);

    return {framebuffer, texture};
}

//General functions
function createShader(gl, type, source){
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)){
        showAlert('Shader compile failed with:' + gl.getShaderInfoLog(shader));
        gl.deleteShader(shader);
        return null;
    }
    return shader;
}

function createProgram(gl, vertexShader, fragmentShader){
	const program = gl.createProgram();
	gl.attachShader(program, vertexShader);
	gl.attachShader(program, fragmentShader);
	gl.linkProgram(program);
	if(!gl.getProgramParameter(program, gl.LINK_STATUS)){
		showAlert('Program failed to link:' + gl.getProgramInfoLog(program));
		return null;
	}
	return program;
}

function createMesh(gl, program){
	const vertexBuffer = gl.createBuffer();
	gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
	const vertices = new Float32Array([
		-1, -1,
		 1, -1,
		-1,  1,
		 1,  1,
	]);
	gl.bufferData(gl.ARRAY_BUFFER,vertices,gl.STATIC_DRAW);
	const aPos = gl.getAttribLocation(program, 'aPos');
	gl.vertexAttribPointer(aPos, 2, gl.FLOAT, false, 0, 0);
	gl.enableVertexAttribArray(aPos);
}

//Initialization
async function initializeWebGL(){
    const canvas = document.getElementById('glCanvas');
    const gl = canvas.getContext('webgl2');
    if(!gl){
        alert('WebGL2 not supported');
        return;
    }

    gl.clearColor(0.0, 0.0, 0.0, 1.0);

    const vertexShader = createShader(gl, gl.VERTEX_SHADER, vertexShaderSource);
    const fragmentShader = createShader(gl, gl.FRAGMENT_SHADER, fragmentShaderSource);
    const program = createProgram(gl, vertexShader, fragmentShader);

    createMesh(gl, program);
    gl.useProgram(program);
	
	//Mouse
	let mouseX = 0;
	let mouseY = 0;
	canvas.addEventListener('mousemove', function(event){
		const rect = canvas.getBoundingClientRect();
		mouseX = event.clientX - rect.left;
		mouseY = event.clientY - rect.top;
	});
	
	//Uniforms
	const iTime = gl.getUniformLocation(program, `iTime`);
	const iFrame = gl.getUniformLocation(program, `iFrame`);
	const iResolution = gl.getUniformLocation(program, `iResolution`);
	const iHour = gl.getUniformLocation(program, `iHour`);
	const iDate = gl.getUniformLocation(program, `iDate`);
	const iFps = gl.getUniformLocation(program, `iFps`);
	const iMouse = gl.getUniformLocation(program, `iMouse`);
	
	//Textures
	await loadTexture(program, gl, 0);
	
	//Buffers
	const iBackBuffer = gl.getUniformLocation(program, `iBackBuffer`);
	const bufferVertexShader = createShader(gl, gl.VERTEX_SHADER, vertexShaderSource);
	const bufferFragmentShader = createShader(gl, gl.FRAGMENT_SHADER, bufferFragmentSource);
	const bufferProgram = createProgram(gl, bufferVertexShader, bufferFragmentShader);
	
	gl.activeTexture(gl.TEXTURE0);
	const {framebuffer: bufferA, texture: textureA} = createFramebuffer(gl, canvas.width, canvas.height);
	const {framebuffer: bufferB, texture: textureB} = createFramebuffer(gl, canvas.width, canvas.height);
	gl.activeTexture(gl.TEXTURE0);
	
	let currentBuffer = false;
	
	//Buffer resize
	function resizeCanvas(){
		canvas.width = window.innerWidth;
		canvas.height = window.innerHeight;
		gl.viewport(0, 0, gl.drawingBufferWidth, gl.drawingBufferHeight);
		gl.bindTexture(gl.TEXTURE_2D, textureA);
		gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, canvas.width, canvas.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
		
		gl.bindTexture(gl.TEXTURE_2D, textureB);
		gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, canvas.width, canvas.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
		
		if(currentBuffer){
			gl.bindTexture(gl.TEXTURE_2D, textureB);
		}else{
			gl.bindTexture(gl.TEXTURE_2D, textureA);
		}
	}
	
	//No buffer resize
	function resizeCanvas(){
		canvas.width = window.innerWidth;
		canvas.height = window.innerHeight;
		gl.viewport(0, 0, gl.drawingBufferWidth, gl.drawingBufferHeight);
	}
	
	//Resize
	window.addEventListener('resize', resizeCanvas);
	resizeCanvas();
	
	//Fps
	const frameDuration = 1000 / @maxFps;
	
	//Uniforms
	let startTime = performance.now();
	let frameCounter = 0;
	let lastTime = performance.now();
	
	//Auto close
	const startTimeSecs = performance.now();
	
	//Render
	function render(){
		const currentTime = performance.now();
		
		//Auto close
		if(currentTime > startTimeSecs + secondsToClose){
			window.close();
		}
		
		//Buffer
		if(currentBuffer){
			gl.bindFramebuffer(gl.FRAMEBUFFER, bufferA);
		}else{
			gl.bindFramebuffer(gl.FRAMEBUFFER, bufferB);
		}
		gl.clear(gl.COLOR_BUFFER_BIT);
		gl.useProgram(program);
		
		//Uniforms
		gl.uniform1f(iTime, (currentTime - startTime) / 1000.0);
		gl.uniform1i(iFrame, frameCounter);
		gl.uniform2f(iResolution, canvas.width, canvas.height);
		const dateHour = new Date();
		gl.uniform3f(iHour, dateHour.getHours(), dateHour.getMinutes(), dateHour.getSeconds());
		const dateDate = new Date();
		gl.uniform3f(iDate, dateDate.getDate(), dateDate.getMonth() + 1, dateDate.getFullYear());
		const deltaTime = (currentTime - lastTime) / 1000;
		lastTime = currentTime;
		gl.uniform1f(iFps, 1 / deltaTime);
		gl.uniform2f(iMouse, (mouseX / canvas.width) * 2 - 1, -((mouseY / canvas.height) * 2 - 1));
		
		//Rendering
		gl.clear(gl.COLOR_BUFFER_BIT);
		gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
		
		//Buffer
		gl.bindFramebuffer(gl.FRAMEBUFFER, null);
		gl.useProgram(bufferProgram);
		if(currentBuffer){
			gl.bindTexture(gl.TEXTURE_2D, textureA);
		}else{
			gl.bindTexture(gl.TEXTURE_2D, textureB);
		}
		gl.clear(gl.COLOR_BUFFER_BIT);
		gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
		currentBuffer = !currentBuffer;
		
		//Max Fps
		const delay = Math.max(0, frameDuration - (performance.now() - currentTime));
		setTimeout(() => {requestAnimationFrame(render);}, delay);
	}
	render();
}
initializeWebGL();