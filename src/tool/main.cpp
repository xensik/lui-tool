// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "stdinc.hpp"

auto overwrite_prompt(const std::string& file) -> bool
{
    auto overwrite = true;

    if (std::filesystem::exists(file))
    {
        do
        {
            printf("File \"%s\" already exists, overwrite? [Y/n]: ", file.data());
            auto result = std::getchar();

            if (result == '\n' || result == 'Y' || result == 'y')
            {
                break;
            }
            else if (result == 'N' || result == 'n')
            {
                overwrite = false;
                break;
            }
        } while (true);
    }

    return overwrite;
}

void disassemble_file(lui::disassembler& disassembler, std::string file)
{
    const auto ext = std::string(".luac");
    const auto extpos = file.find(ext);
    
    if (extpos != std::string::npos)
    {
        file.replace(extpos, ext.length(), "");
    }

    auto data = utils::file::read(file + ".luac");

    disassembler.disassemble(data);
    
    utils::file::save(file + ".luasm", disassembler.output());
}

int parse_flags(int argc, char** argv, game& game, mode& mode)
{
    if (argc != 4) return 1;

    std::string arg = utils::string::to_lower(argv[1]);

    if (arg == "-iw6")
    {
        game = game::IW6;
    }
    else
    {
        printf("Unknown game \"%s\".\n", argv[1]);
        return 1;
    }

    arg = utils::string::to_lower(argv[2]);

    if (arg == "-disasm")
    {
        mode = mode::DISASM;
    }
    else
    {
        printf("Unknown mode \"%s\".\n\n", argv[2]);
        return 1;
    }

    return 0;
}

int main(int argc, char** argv)
{
    std::string file = argv[argc - 1];
    mode mode = mode::__;
    game game = game::__;

    if (parse_flags(argc, argv, game, mode))
    {
        printf("usage: lui-tool.exe <game> <mode> <file>\n");
        printf("	- games: -iw6\n");
        printf("	- modes: -disasm\n");
        return 0;
    }

    if (mode == mode::DISASM)
    {
        if (game == game::IW6)
        {
            IW6::disassembler disassembler;
            disassemble_file(disassembler, file);
        }
    }

    return 0;
}
