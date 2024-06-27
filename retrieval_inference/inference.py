import sys
import os
import numpy as np
import hydra
import omegaconf
from pytorch_lightning import (
    LightningModule,
    seed_everything,
)
from pytorch_lightning.utilities.cloud_io import load as pl_load
import torch
import torch.nn.functional as F


point_num = 1024

def interpolate(v0, v1, step):
    dv = v0 - v1
    length = np.linalg.norm(dv)
    length_list = np.arange(0, length, step)
    point_list = [v0]
    for l_i in length_list:
        point_list.append(v1 + dv/length * l_i)
    return point_list

def read_obj(model_path):
    objFile = open(model_path, 'r')
    vertexList = []
    lineList = []
    for line in objFile:
        split = line.split()
        # if blank line, skip
        if not len(split):
            continue
        if split[0] == "v":
            vertexList.append([float(split[1]), float(split[2]), float(split[3])])
        elif split[0] == "l":
            lineList.append(split[1:])
    objFile.close()
    return vertexList, lineList

def sample_pointcloud_edge(model_path):
    vertexList, lineList = read_obj(model_path)
    if len(vertexList) < 1 or len(lineList) < 2:
        return None
    sum_length = 0
    for edge in lineList:
        v0 = np.array(vertexList[int(edge[0])-1])
        v1 = np.array(vertexList[int(edge[1])-1])
        sum_length += np.linalg.norm(v0 - v1)
    step = sum_length / point_num

    point_list = []
    for edge in lineList:
        v0 = np.array(vertexList[int(edge[0])-1])
        v1 = np.array(vertexList[int(edge[1])-1])
        point_list.extend(interpolate(v0, v1, step))
    sample_index = np.random.choice(len(point_list), point_num, replace=False)
    new_point_list = np.array(point_list)[sample_index]
    return new_point_list

def normalize_to_box(input):
    """
    normalize point cloud to unit bounding box
    center = (max - min)/2
    scale = max(abs(x))
    input: pc [N, P, dim] or [P, dim]
    output: pc, centroid, furthest_distance

    From https://github.com/yifita/pytorch_points
    """
    if len(input.shape) == 2:
        axis = 0
        P = input.shape[0]
        D = input.shape[1]
    elif len(input.shape) == 3:
        axis = 1
        P = input.shape[1]
        D = input.shape[2]
    else:
        raise ValueError()
    
    if isinstance(input, np.ndarray):
        maxP = np.amax(input, axis=axis, keepdims=True)
        minP = np.amin(input, axis=axis, keepdims=True)
        centroid = (maxP+minP)/2
        input = input - centroid
        furthest_distance = np.amax(np.abs(input), axis=(axis, -1), keepdims=True)
        input = input / furthest_distance
    elif isinstance(input, torch.Tensor):
        maxP = torch.max(input, dim=axis, keepdim=True)[0]
        minP = torch.min(input, dim=axis, keepdim=True)[0]
        centroid = (maxP+minP)/2
        input = input - centroid
        in_shape = list(input.shape[:axis])+[P*D]
        furthest_distance = torch.max(torch.abs(input).reshape(in_shape), dim=axis, keepdim=True)[0]
        furthest_distance = furthest_distance.unsqueeze(-1)
        input = input / furthest_distance
    else:
        raise ValueError()

    return input, centroid, furthest_distance

def rotate_point_cloud(batch_data, dim='x', angle=-90): # torch.Size([1024, 3])
    rotation_angle = angle/360 * 2 * np.pi
    cosval = np.cos(rotation_angle)
    sinval = np.sin(rotation_angle)
    if dim=='x':
        rotation_matrix = torch.tensor([[1, 0, 0],
                                [0, cosval, -sinval],
                                [0, sinval, cosval]]).float()
    elif dim=='y':
        rotation_matrix = torch.tensor([[cosval, 0, sinval],
                                    [0, 1, 0],
                                    [-sinval, 0, cosval]]).float()
    elif dim=='z':
        rotation_matrix = torch.tensor([[cosval, -sinval, 0],
                                    [sinval, cosval, 0],
                                    [0, 0, 1]]).float()
    else:
        NotImplementedError
        
    rotated_data = torch.mm(batch_data, rotation_matrix)
    return rotated_data # torch.Size([1024, 3])

def get_retrieval_model():
    # Load Retrieval model
    path = 'model/config.yaml'
    cfg = omegaconf.OmegaConf.load(path)
    checkpoint_file = 'model/last.ckpt'
    print(f"Instantiating model <{cfg.model._target_}>")
    model: LightningModule = hydra.utils.instantiate(cfg.model)
    ckpt = pl_load(checkpoint_file, map_location=lambda storage, loc: storage)  
    model.load_state_dict(ckpt['state_dict'])

    print('Load retrieval model from: {}'.format(checkpoint_file))
    model = model.eval() #.cuda()
    return model

def compute_distance(a, b, l2=True):
    if l2:
        a = F.normalize(a, p=2, dim=1)
        b = F.normalize(b, p=2, dim=1)
    
    distance = torch.cdist(a, b, p=2)
    return distance

def predict(model, sketch_path):
    point_list = sample_pointcloud_edge(sketch_path).astype(np.float32)
    #TODO: normalize point cloud
    pc, center, scale = normalize_to_box(point_list)
    pc = rotate_point_cloud(torch.tensor(pc), dim='y', angle=-90)

    shape = pc.unsqueeze(0) #.cuda()
    sketch_feat = model.encoder(shape.transpose(1, 2))
    # Compute distance 
    shape_feat = torch.tensor(np.load('model/test_shape_last_feat.npy')) #.cuda()
    d_feat_z = compute_distance(sketch_feat, shape_feat, l2=True)
    pair_sort = torch.argsort(d_feat_z, dim=1)[0].detach().data.cpu().numpy()

    list_file = 'test_shape.txt'
    name_list = [line.rstrip().split(' ')[0] for line in open(list_file)]
    final_names = [name_list[index] for index in pair_sort[:10]]
    print("Finish Inference!")
    return ' '.join(final_names[:5])


if __name__ == '__main__':
    argv = sys.argv
    argv = argv[argv.index("--") + 1:]
    sketch_path = argv[0]
    # sketch_path = '/media/ll00931/Ling/dataset/multi_category/synthetic_sketch/02691156/1021a0914a7207aff927ed529ad90a11_network_20_aggredated_sketch_0.0.obj'
    #TODO: Load Model 
    model = get_retrieval_model()

    index = predict(model, sketch_path)
    print(index)

