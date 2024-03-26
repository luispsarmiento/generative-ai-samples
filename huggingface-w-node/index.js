import { HfInference } from '@huggingface/inference';
import { config } from 'dotenv';
import * as fs from 'fs';

config();

const hf = new HfInference(process.env.HF_ACCESS_TOKEN);

const saveImageFromBuffer = (buffer, filename) => {
    fs.writeFile(`./images/${filename}`, buffer, (err) => {
        if (err) {
            return console.error(`Error saving the image: ${err.message}`);
        }
        console.log('Image saved successfully!');
    });
};

const imageResult = await hf.textToImage({
    inputs: "Design an amazing logo in high resolution that represents AI, Cloud Computing, and Cybersecurity",
    model: 'stabilityai/stable-diffusion-2-1-base',
    parameters: {
        negative_prompt: 'blurry',
    }
});

const arrayBuffer = await imageResult.arrayBuffer();
const buffer = Buffer.from(arrayBuffer);

saveImageFromBuffer(buffer, 'amazing_logo_2.png');