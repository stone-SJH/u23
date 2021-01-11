import ShaderChunk from '../shaders/ShaderChunk';
import pbrVS from '../shaders/pbrVS';
import pbrFS from '../shaders/pbrFS';
import wireframeVS from '../shaders/wireframeVS'
import wireframeFS from '../shaders/wireframeFS'
import glslify from 'glslify'

export default class Program {
	constructor() {
		this.includePattern = /^[ \t]*#preImport +<([\w\d./]+)>/gm;
	}

	getPBRShader() {
		let vs = this.preParseShader(pbrVS);
		let fs = this.preParseShader(pbrFS);
		return {
			pbrVS: vs,
			pbrFS: fs
		};
	}

	getWireframeShader(){
		let vs = glslify(wireframeVS);
		let fs = glslify(wireframeFS);
		return {
			wireVS: vs,
			wireFS: fs
		};
	}
	
	preParseShader(string) {
		let includeReplacer = (match, include) => {
			let string = ShaderChunk[include];
			if (string === undefined) {
				throw new Error('Can not resolve #preImport <' + include + '>');
			}
			return this.preParseShader(string);
		};
		return string.replace(this.includePattern, includeReplacer);
	}
}
